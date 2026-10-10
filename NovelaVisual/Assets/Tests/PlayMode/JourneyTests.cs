using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CaliNature.Tests
{
    // Integration tests drive existing behaviours and walk the saved scenes.
    // No scenario objects are created. Save writes use a unique test-only key.
    public sealed class JourneyTests
    {
        private string testSession;
        private GameSession session;
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        [UnitySetUp] public IEnumerator Prepare()
        {
            testSession = System.Guid.NewGuid().ToString("N");
            GameSaveManager.TestSessionId = testSession;
            if (GameSession.Instance) { Object.Destroy(GameSession.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("01_MainMenu"); yield return null;
            session = GameSession.Instance;
            Assert.IsNotNull(session); session.Save.ResetProgress();
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            if (GameSession.Instance) { Object.Destroy(GameSession.Instance.gameObject); yield return null; }
            PlayerPrefs.DeleteKey("CaliNature.Tests." + testSession);
            PlayerPrefs.Save();
            GameSaveManager.TestSessionId = null;
        }
        private static void CloseModal(GameSession s) => typeof(CaliGameUI).GetMethod("CloseModal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(s.UI, null);
        private static KeyCode Key(Vector2 direction) => direction.y > 0 ? KeyCode.W : direction.y < 0 ? KeyCode.S : direction.x > 0 ? KeyCode.D : KeyCode.A;
        private void Tap(KeyCode key) { session.Input.KeyPressed(key); session.Input.KeyReleased(key); }
        private IEnumerator Mode(GameMode mode)
        {
            float end = Time.realtimeSinceStartup + 15;
            while (session.State.Mode != mode && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(mode, session.State.Mode);
        }
        private static bool ClearRay(Vector2 origin, WorldInteractable item, PlayerGridMovement player)
        {
            Vector2 delta = (Vector2)item.transform.position-origin;
            return !Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude).Any(h => !h.collider.isTrigger && !h.transform.IsChildOf(player.transform) && !h.transform.IsChildOf(item.transform));
        }
        private static bool CanStep(Vector2 at, Vector2 direction, PlayerGridMovement player)
        {
            var collider = player.GetComponent<BoxCollider2D>();
            Vector2 offset = (Vector2)collider.bounds.center - (Vector2)player.transform.position;
            return !Physics2D.BoxCastAll(at + offset, collider.bounds.size, 0, direction, .515f).Any(h => !h.collider.isTrigger && !h.transform.IsChildOf(player.transform) && h.distance < .51f);
        }
        private static List<Vector2> Route(PlayerGridMovement player, WorldInteractable target)
        {
            Vector2 start = player.transform.position;
            var queue = new Queue<Vector2Int>(); var seen = new HashSet<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(Vector2Int.zero); seen.Add(Vector2Int.zero);
            while (queue.Count > 0 && seen.Count < 8000)
            {
                var node = queue.Dequeue(); Vector2 at = start + (Vector2)node * .5f;
                Vector2 facing = node == Vector2Int.zero ? player.Facing : (Vector2)(node - previous[node]);
                if (InputRules.IsInFront(at, facing, target.transform.position, target.Range) && ClearRay(at,target,player))
                {
                    var route = new List<Vector2>();
                    while (node != Vector2Int.zero) { var parent = previous[node]; route.Add(node-parent); node=parent; }
                    route.Reverse(); return route;
                }
                foreach (var direction in Directions)
                {
                    var next = node + direction;
                    if (seen.Contains(next) || Mathf.Abs(next.x)>65 || Mathf.Abs(next.y)>65 || !CanStep(at,direction,player)) continue;
                    seen.Add(next); previous[next]=node; queue.Enqueue(next);
                }
            }
            Assert.Fail("No walkable frontal route to " + target.name + " from " + start); return null;
        }
        private IEnumerator Walk(PlayerGridMovement player, WorldInteractable target)
        {
            var route = Route(player,target);
            Debug.Log("CALI_JOURNEY walking " + target.name + " steps=" + route.Count);
            foreach (var direction in route)
            {
                Vector2 destination = (Vector2)player.transform.position + direction*.5f;
                Tap(Key(direction));
                float deadline = Time.realtimeSinceStartup+2;
                do { yield return null; } while ((player.IsStepping || Vector2.Distance(player.transform.position,destination)>.03f) && Time.realtimeSinceStartup<deadline);
                Assert.Less(Vector2.Distance(player.transform.position,destination),.04f,"Blocked on route to "+target.name);
            }
            yield return null;
            Assert.IsTrue(InputRules.IsInFront(player.transform.position,player.Facing,target.transform.position,target.Range));
            Assert.IsNotEmpty(player.GetComponent<PlayerInteraction>().CurrentPrompt);
        }
        private IEnumerator Conversation(PlayerGridMovement player, NPCDialogueController npc)
        {
            yield return Walk(player,npc);
            Vector3 position=player.transform.position;
            Tap(KeyCode.E); yield return Mode(GameMode.Dialogue);
            var first=session.Dialogue.Current;
            Tap(KeyCode.E); yield return null; Assert.AreSame(first,session.Dialogue.Current,"E advanced conversation");
            Tap(KeyCode.W); yield return new WaitForSeconds(.1f); Assert.Less(Vector3.Distance(position,player.transform.position),.01f);
            int heard=session.Save.Data.heardNarrations.Count;
            session.Input.KeyPressed(KeyCode.Space); yield return null; yield return null;
            for(int i=0;i<5;i++){session.Input.KeyPressed(KeyCode.Space);yield return null;}
            Assert.AreSame(first,session.Dialogue.Current,"Held Space skipped a node");
            Assert.AreEqual(heard,session.Save.Data.heardNarrations.Count,"Revealing registered an incomplete voice");
            session.Input.KeyReleased(KeyCode.Space);
            int guard=0;
            while(session.State.Mode==GameMode.Dialogue && guard++<30)
            {
                if(session.Dialogue.ShowingChoices)
                {
                    var current=session.Dialogue.Current; Tap(KeyCode.Space); yield return null;
                    Assert.AreSame(current,session.Dialogue.Current,"Space chose an answer");
                    int choice=System.Array.FindIndex(current.choices,c=>c.responsible && session.Save.HasClue(c.requiredClue));
                    Assert.GreaterOrEqual(choice,0); session.Dialogue.Choose(choice);
                }
                else Tap(KeyCode.Space);
                yield return null; yield return null;
            }
            Assert.Less(guard,30,"Dialogue did not terminate");
            Assert.Less(Vector3.Distance(position,player.transform.position),.01f,"Dialogue moved player");
            Debug.Log("CALI_JOURNEY conversation " + npc.name + " completed");
        }
        [UnityTest, Timeout(600000)] public IEnumerator FourSavedZonesCanBeWalkedAndCompleted()
        {
            session.StartIntroduction(); Assert.AreEqual(GameMode.Introduction,session.State.Mode);
            session.OpenMap(); yield return null;
            Assert.AreEqual(4,Object.FindObjectsByType<CaliMapZoneSelector>(FindObjectsSortMode.None).Length);
            var nav=Object.FindAnyObjectByType<CaliMapNavigation>();
            Vector2 point=new Vector2(Screen.width*.5f,Screen.height*.5f);
            Assert.IsTrue(nav.BeginPointer(0,point)); Vector3 before=nav.transform.position;
            nav.EndPointer(0,point+new Vector2(70,30));
            Assert.Greater(Vector3.Distance(before,nav.transform.position),.1f,"Final drag displacement lost");
            Assert.IsNull(session.SelectedChapter,"Drag selected destination");
            float zoom=Camera.main.orthographicSize;nav.Zoom(-1);Assert.Less(Camera.main.orthographicSize,zoom);
            for(int chapter=0;chapter<4;chapter++)
            {
                session.SelectChapter(chapter); session.EnterSelected(); yield return Mode(GameMode.Cinematic);
                session.Cinematic.Skip(); yield return null;
                if(chapter==0){Assert.AreEqual(GameMode.Pause,session.State.Mode);CloseModal(session);}
                yield return Mode(GameMode.Exploration);
                Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
                var player=Object.FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None).Single(p=>p.enabled);
                var entry=Object.FindAnyObjectByType<ZoneEntryPoint>();
                Assert.Less(Vector3.Distance(entry.transform.position,player.transform.position),.01f);
                Assert.IsNotNull(Camera.main.GetComponent<PlayerCameraFollow>());
                var clues=Object.FindObjectsByType<ClueInteractable>(FindObjectsSortMode.None).OrderBy(x=>x.name).ToArray();
                Assert.AreEqual(3,clues.Length);
                foreach(var clue in clues)
                {
                    yield return Walk(player,clue);int count=session.Save.Data.clues.Count;
                    Tap(KeyCode.E);yield return Mode(GameMode.Pause);Assert.AreEqual(count+1,session.Save.Data.clues.Count);CloseModal(session);
                    Tap(KeyCode.E);yield return Mode(GameMode.Pause);Assert.AreEqual(count+1,session.Save.Data.clues.Count);CloseModal(session);
                }
                var npcs=Object.FindObjectsByType<NPCDialogueController>(FindObjectsSortMode.None).OrderBy(n=>n.name).ToArray();
                Assert.AreEqual(3,npcs.Length);
                foreach(var npc in npcs)yield return Conversation(player,npc);
                Assert.AreEqual(GameMode.Summary,session.State.Mode);
                Assert.AreEqual(chapter+1,session.Save.Data.completedChapters.Count);
                int decisions=session.Save.Data.decisions.Count;session.CheckChapterCompletion();Assert.AreEqual(decisions,session.Save.Data.decisions.Count);
                session.ReturnToMap();yield return Mode(GameMode.Map);yield return null;
                Assert.AreEqual(1,Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
                session.Save.Load();Assert.AreEqual(chapter+1,session.Save.Data.completedChapters.Count);
                Debug.Log("CALI_JOURNEY chapter "+chapter+" saved and returned to map");
            }
            Assert.AreEqual(12,session.Save.Data.clues.Count);Assert.AreEqual(12,session.Save.Data.decisions.Count);
        }
    }
}
