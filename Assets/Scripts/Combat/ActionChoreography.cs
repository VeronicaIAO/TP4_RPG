using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    public class ActionChoreography : MonoBehaviour
    {
        public FeedbackManager feedback;

        public string punchClip = "attack-melee-right";
        public string reachClip = "interact-right";
        public string throwClip = "attack-melee-right";
        public string nodClip = "emote-yes";
        [Range(0.1f, 0.9f)] public float punchImpactPoint = 0.45f;

        BattleUnit user;
        ActionData action;
        List<BattleUnit> targets;
        Action applyEffects;
        float s;
        bool impacted;

        public IEnumerator Play(ActionData a, BattleUnit u, List<BattleUnit> t, Action apply, float speed)
        {
            action = a;
            user = u;
            targets = t;
            applyEffects = apply;
            s = Mathf.Max(0.1f, speed);
            impacted = false;

            if (u.Voice != null) u.Voice.PlayAttack(a);

            switch (a.actionAnimation)
            {
                case ActionAnimation.Punch: yield return Punch(); break;
                case ActionAnimation.Shove: yield return Shove(); break;
                case ActionAnimation.HairPull: yield return HairPull(); break;
                case ActionAnimation.Insult: yield return Insult(); break;
                case ActionAnimation.Callout: yield return Callout(); break;
                case ActionAnimation.Defamation: yield return Defamation(); break;
                case ActionAnimation.Spit: yield return Spit(); break;
                case ActionAnimation.Throw: yield return Throw(); break;
                case ActionAnimation.Consume: yield return Consume(); break;
                case ActionAnimation.Guard:
                    Impact();
                    yield return Wait(0.45f);
                    break;
                default:
                    Impact();
                    yield return Wait(0.35f);
                    break;
            }

            if (!impacted) Impact();
            V.SetTalking(false);
            V.ShowProp(false);
            V.PlayIdle();
        }

        UnitVisual V => user.Visual;
        WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds / s);
        float D(float seconds) => seconds / s;

        BattleUnit FirstOther
        {
            get
            {
                foreach (var t in targets) if (t != user && t.IsAlive) return t;
                return null;
            }
        }

        void Impact()
        {
            if (impacted) return;
            impacted = true;
            applyEffects?.Invoke();
            if (action.reaction == HitReaction.None || !ActionResolver.TargetsRivals(action.target)) return;
            foreach (var t in targets)
            {
                if (t == null || t == user || !t.IsAlive) continue;
                t.Visual.React(action.reaction, user.transform.position, action.knockback, s);
                if (t.Voice != null) t.Voice.PlayReaction(action.reaction);
            }
        }

        IEnumerator BackToRest()
        {
            yield return V.PoseTo(V.RestPose, D(0.15f));
        }

        void Sound(FeedbackSound sound)
        {
            if (feedback != null) feedback.Sound(sound);
        }

        IEnumerator ClipWithImpact(string clipName, float impactAt, float fallback)
        {
            float length = V.ClipLength(clipName);
            if (length <= 0f) length = fallback;
            V.PlayClip(clipName, false, 0.05f, s);
            yield return new WaitForSeconds(D(length * impactAt));
            Impact();
            yield return new WaitForSeconds(D(length * (1f - impactAt)));
        }

        IEnumerator Punch()
        {
            yield return V.PoseTo(Poses.PunchWindup, D(0.12f));
            Sound(FeedbackSound.Whoosh);
            StartCoroutine(V.PoseTo(Poses.PunchStrike, D(0.1f)));
            yield return ClipWithImpact(punchClip, punchImpactPoint, 0.4f);
            yield return Wait(0.2f);
            yield return BackToRest();
        }

        IEnumerator Shove()
        {
            yield return V.PoseTo(Poses.ShoveWindup, D(0.18f));
            Sound(FeedbackSound.Whoosh);
            var lunge = StartCoroutine(V.Lunge(0.25f * V.model.localScale.y, D(0.08f)));
            yield return V.PoseTo(Poses.ShoveStrike, D(0.07f));
            yield return lunge;
            Impact();
            yield return Wait(0.4f);
            yield return BackToRest();
            yield return V.MoveTo(V.HomePosition, D(0.2f), false);
        }

        IEnumerator HairPull()
        {
            var victim = FirstOther;
            V.PlayClip(reachClip, false, 0.08f, s);
            yield return V.PoseTo(Poses.HairReach, D(0.2f));
            if (victim != null)
            {
                victim.Visual.SetHeldByHair(true, s);
                if (victim.Voice != null) victim.Voice.PlayReaction(HitReaction.Surprise);
            }
            yield return Wait(0.08f);

            for (int i = 0; i < 3; i++)
            {
                Sound(FeedbackSound.Whoosh);
                if (victim != null) victim.Visual.HairJerk(user.transform.position, 0.18f * V.model.localScale.y, D(0.16f));
                yield return V.PoseTo(Poses.HairYank, D(0.08f));
                yield return V.PoseTo(Poses.HairHold, D(0.1f));
            }

            yield return V.PoseTo(Poses.HairYank, D(0.08f));
            if (victim != null) victim.Visual.SetHeldByHair(false, s);
            Impact();
            yield return Wait(0.45f);
            yield return BackToRest();
        }

        IEnumerator Insult()
        {
            yield return V.PoseTo(Poses.InsultPoint, D(0.15f));
            V.SetTalking(true);
            Sound(FeedbackSound.Verbal);
            for (int i = 0; i < 3; i++)
            {
                yield return V.PoseTo(Poses.InsultJab, D(0.1f));
                yield return V.PoseTo(Poses.InsultPoint, D(0.12f));
            }
            Impact();
            yield return Wait(0.35f);
            V.SetTalking(false);
            yield return BackToRest();
        }

        IEnumerator Callout()
        {
            V.ShowProp(true);
            yield return V.PoseTo(Poses.CalloutPhoneUp, D(0.22f));
            yield return Wait(0.15f);
            Sound(FeedbackSound.Shutter);
            if (feedback != null) feedback.CameraFlash(V.RightHandPoint);
            foreach (var t in targets) if (t != user) t.Visual.Flash(Color.white, 0.35f);
            yield return Wait(0.25f);
            yield return V.PoseTo(Poses.CalloutPoint, D(0.14f));
            V.SetTalking(true);
            Sound(FeedbackSound.Verbal);
            yield return Wait(0.6f);
            Impact();
            yield return Wait(0.4f);
            V.SetTalking(false);
            V.ShowProp(false);
            yield return BackToRest();
        }

        IEnumerator Defamation()
        {
            float sign = V.AudienceSign;
            yield return V.PoseTo(Poses.Whisper(sign), D(0.25f));
            V.SetTalking(true);
            yield return Wait(0.7f);
            V.SetTalking(false);
            V.PlayClip(nodClip, false, 0.1f, s);
            yield return V.PoseTo(Poses.DefamePoint, D(0.15f));
            V.SetTalking(true);
            Sound(FeedbackSound.Verbal);
            yield return Wait(0.6f);
            Impact();
            yield return Wait(0.4f);
            V.SetTalking(false);
            yield return BackToRest();
        }

        IEnumerator Spit()
        {
            yield return V.PoseTo(Poses.SpitWindup, D(0.28f));
            yield return V.PoseTo(Poses.SpitRelease, D(0.06f));
            float flight = D(0.4f);
            if (feedback != null)
                foreach (var t in targets)
                    if (t != user) StartCoroutine(feedback.Projectile(V.MouthPoint, t.Visual.HeadPoint, action.effectColor, flight, 0.08f, 0.3f));
            yield return new WaitForSeconds(flight);
            Impact();
            yield return Wait(0.35f);
            yield return BackToRest();
        }

        IEnumerator Throw()
        {
            yield return V.PoseTo(Poses.ThrowWindup, D(0.2f));
            Sound(FeedbackSound.Whoosh);
            V.PlayClip(throwClip, false, 0.05f, s);
            yield return V.PoseTo(Poses.ThrowRelease, D(0.07f));
            float flight = D(0.45f);
            if (feedback != null)
                foreach (var t in targets)
                    if (t != user) StartCoroutine(feedback.Projectile(V.RightHandPoint, t.Visual.ImpactPoint, action.effectColor, flight, 0.2f, 0.8f));
            yield return new WaitForSeconds(flight);
            Impact();
            yield return Wait(0.35f);
            yield return V.PoseTo(V.RestPose, D(0.18f));
        }

        IEnumerator Consume()
        {
            V.PlayClip(reachClip, false, 0.08f, s);
            yield return V.PoseTo(Poses.Consume, D(0.25f));
            if (feedback != null) foreach (var t in targets) feedback.Aura(t.transform.position, action.effectColor);
            yield return Wait(0.35f);
            Impact();
            yield return Wait(0.3f);
            yield return V.PoseTo(V.RestPose, D(0.2f));
        }
    }
}
