using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    public class ActionChoreography : MonoBehaviour
    {
        public FeedbackManager feedback;
        public float windupTime = 0.15f;
        public float strikeTime = 0.08f;
        public float holdTime = 0.3f;

        public IEnumerator Play(ActionData a, BattleUnit user, List<BattleUnit> targets, Action applyEffects, float speed)
        {
            float s = Mathf.Max(0.1f, speed);
            var v = user.Visual;
            if (user.Voice != null) user.Voice.PlayAttack(a);

            if (TryGetPoses(a.actionAnimation, v, out BodyPose windup, out BodyPose strike))
            {
                yield return v.PoseTo(windup, windupTime / s);
                if (feedback != null) feedback.Sound(a.actionAnimation == ActionAnimation.Insult || a.actionAnimation == ActionAnimation.Callout || a.actionAnimation == ActionAnimation.Defamation ? FeedbackSound.Verbal : FeedbackSound.Whoosh);
                yield return v.PoseTo(strike, strikeTime / s);
            }

            applyEffects?.Invoke();
            if (a.reaction != HitReaction.None && ActionResolver.TargetsRivals(a.target))
                foreach (var t in targets)
                    if (t != null && t != user && t.IsAlive && t.Voice != null) t.Voice.PlayReaction(a.reaction);

            yield return new WaitForSeconds(holdTime / s);
            yield return v.PoseTo(Poses.Idle, windupTime / s);
        }

        static bool TryGetPoses(ActionAnimation anim, UnitVisual v, out BodyPose windup, out BodyPose strike)
        {
            switch (anim)
            {
                case ActionAnimation.Punch: windup = Poses.PunchWindup; strike = Poses.PunchStrike; return true;
                case ActionAnimation.Shove: windup = Poses.ShoveWindup; strike = Poses.ShoveStrike; return true;
                case ActionAnimation.HairPull: windup = Poses.HairReach; strike = Poses.HairYank; return true;
                case ActionAnimation.Insult: windup = Poses.InsultPoint; strike = Poses.InsultJab; return true;
                case ActionAnimation.Callout: windup = Poses.CalloutPhoneUp; strike = Poses.CalloutPoint; return true;
                case ActionAnimation.Defamation: windup = Poses.Whisper(v.AudienceSign); strike = Poses.DefamePoint; return true;
                case ActionAnimation.Spit: windup = Poses.SpitWindup; strike = Poses.SpitRelease; return true;
                default: windup = Poses.Idle; strike = Poses.Idle; return false;
            }
        }
    }
}
