using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CaliNature.Tests
{
    public sealed class ControlsAndStoryTests
    {
        [Test] public void DiagonalInputResolvesToOneCardinalDirection() { Assert.AreEqual(Vector2.up, InputRules.Cardinal(Vector2.one)); }
        [Test] public void ZeroInputStops() { Assert.AreEqual(Vector2.zero, InputRules.Cardinal(Vector2.zero)); }
        [Test] public void NPCBehindCannotBeSelected() { Assert.IsFalse(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.down, 2)); }
        [Test] public void NPCOutsideRangeCannotBeSelected() { Assert.IsFalse(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.up * 3, 2)); }
        [Test] public void NPCInFrontWithinRangeCanBeSelected() { Assert.IsTrue(InputRules.IsInFront(Vector2.zero, Vector2.up, Vector2.up, 2)); }
        [Test] public void OpeningFrameCannotAdvance() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(true, true, false, 42, 42)); }
        [Test] public void FirstSpaceRevealsText() { Assert.AreEqual(DialogueAdvanceAction.Reveal, InputRules.Advance(true, true, false, 43, 42)); }
        [Test] public void SecondSpaceAdvancesCompletedLine() { Assert.AreEqual(DialogueAdvanceAction.Next, InputRules.Advance(true, false, false, 44, 42)); }
        [Test] public void SpaceNeverSelectsAnAnswer() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(true, false, true, 44, 42)); }
        [Test] public void BlockedDialogueCannotAdvance() { Assert.AreEqual(DialogueAdvanceAction.None, InputRules.Advance(false, false, false, 44, 42)); }
        [Test] public void ShortGestureIsClick() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Move(Vector2.one * 2, 10); Assert.IsTrue(g.End(Vector2.one * 2, 10)); }
        [Test] public void DragCannotBecomeClickWhenReturningToStart() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Move(Vector2.right * 20, 10); Assert.IsFalse(g.End(Vector2.zero, 10)); }
        [Test] public void ReleaseAfterLargeMoveIsNotClickEvenWithoutMoveEvent() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); Assert.IsFalse(g.End(Vector2.right * 50, 10)); }
        [Test] public void CancelledGestureCannotSelect() { var g = new MapPointerGesture(); g.Begin(Vector2.zero); g.Cancel(); Assert.IsFalse(g.End(Vector2.zero, 10)); }
        [Test] public void ReleaseWithoutPressCannotSelect() { Assert.IsFalse(new MapPointerGesture().End(Vector2.zero, 10)); }
        [Test] public void FourChaptersHaveTwelveCluesAndTwelveDecisions()
        {
            var data = JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("CaliNature/StoryCatalog").text);
            Assert.AreEqual(4, data.chapters.Length);
            Assert.AreEqual(12, data.chapters.SelectMany(c => c.clues).Select(c => c.id).Distinct().Count());
            Assert.AreEqual(12, data.chapters.SelectMany(c => c.conversations).SelectMany(c => c.nodes).SelectMany(n => n.choices).Select(c => c.decisionKey).Distinct().Count());
            foreach (var chapter in data.chapters)
            {
                Assert.AreEqual(3, chapter.conversations.Length);
                foreach (var conversation in chapter.conversations)
                {
                    var ids = conversation.nodes.Select(n => n.id).ToArray();
                    Assert.Contains(conversation.firstNode, ids);
                    foreach (var node in conversation.nodes)
                    {
                        if (!string.IsNullOrEmpty(node.next)) Assert.Contains(node.next, ids);
                        foreach (var choice in node.choices) Assert.Contains(choice.next, ids);
                    }
                }
            }
        }
    }
}
