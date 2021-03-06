// Behavior originally contributed by Chinajade.
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.

#region Summary and Documentation
// QUICK DOX:
// ESCORTGROUP has the following characteristics:
//      * Escorts and defends a single NPC or a group of NPCs
//      * Can be used to defend a stationary object
//      * Can gossip with an NPC to initiate the quest (outside of a quest pickup)
//      * Will wait for the NPCs to be escorted to arrive, or can search for them
//          via a supplied SearchPath.
//      * While escorting the group, it 'leads' the group in a very human-like fashion
//
//  BEHAVIOR ATTRIBUTES:
//  Interaction to start:
//      StartNpcIdN [REQUIRED if StartEscortGossipOptions specified]
//          N may be omitted, or any numeric value--multiple mobs are supported.
//          If NPC interaction is required to start the escort, the StartNpcIdN identifieds one
//          or more NPCs that may be interacted with to initiate the escort.
//          If the quest fails for some reason, the behavior will return to these NPCs
//          to try again.
//      StartEscortGossipOptions [optional; Default: a dialog-less chat interaction]
//          This argument is only used of a StartNpcIdN is specified.
//          Specifies the set of interaction choices on each gossip panel to make, in order to
//          start the escort.
//          The value of this attribute is a comma-separated list of gossip options (e.g., "1,3,1").
//
// NPCs to escort:
//      EscortNpcIdN [REQUIRED]
//          N may be omitted, or any numeric value--multiple mobs are supported.
//          Identifies one or more NPCs to be escorted.
//
// Completion Criteria:
//      EscortCompleteWhen [optional; Default: QuestComplete]
//          [allowed values: DestinationReached/QuestComplete/QuestCompleteOrFails]
//          Defines the completion criteria for the behavior.
//      EscortCompleteX/EscortCompleteY/EscortCompleteZ [REQUIRED if EscortCompleteWhen=DestinationReached]
//          Defines the location at which the behavior is considered complete.  This value is only used
//          if EscortComplete is DestinationReached.
//
// Quest binding:
//      QuestId [REQUIRED if EscortCompleteWhen=QuestComplete; Default:none]:
//      QuestCompleteRequirement [Default:NotComplete]:
//      QuestInLogRequirement [Default:InLog]:
//              A full discussion of how the Quest* attributes operate is described in
//              http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//      QuestObjectiveIndex [REQUIRED if EventCompleteWhen=QuestObjectiveComplete]
//          [on the closed interval: [1..5]]
//          This argument is only consulted if EventCompleteWhen is QuestObjectveComplete.
//          The argument specifies the index of the sub-goal of a quest.
//
// Tunables (ideally, the profile would _never_ provide these arguments):
//      CombatMaxEngagementRangeDistance [optional; Default: 23.0]
//          This is a work around for some buggy Combat Routines.  If a targetted mob is
//          "too far away", some Combat Routines refuse to engage it for killing.  This
//          value moves the toon within an appropriate distance to the requested target
//          so the Combat Routine will perform as expected.
//      DebugReportUnitsOutOfRange [optional; Default: false]
//          When enabled, any hostile targets that are out of EscortMaxFightDistance will be
//          reported with log warnings.
//      EscortCompleteMaxRange [optional; Default: 10.0]
//          Defines the tolerance from the EscortCompleteX/Y/Z for which the behavior is considered
//          complete.  This value is only used if EscortCompelte is DestinationReached.
//      EscortCountMax [optional; Default: 100]
//          This is the maximum number of units the behavior should escort at one time.
//          In some area, there are many NPCs with the same MobId that must be gossiped
//          and escorted to complete a quest.  These mobs are spread out, and if the
//          behavior sees too many at once, it tries to escort them all--even though they
//          may be spread out 50-100 yards.  In those situations, setting this value to 1
//          keeps the behavior focused on the immediate mob that needs escorting, and
//          not try to escort them all.
//      EscortMaxFightDistance [optional; Default: 27.0]
//          [on the close range: [5.0..100.0]]
//          The maximum range from the _nearest_ escorted NPC at which the toon will fight.
//          If for some reason, the toon is pulled outside of this range, the behavior
//          moves the toon back into this maximum range.
//      EscortMaxFollowDistance [optional; Default: 15.0]
//          [on the closed range: [3.0..100.0]]
//          The maximum range at which the toon will follow the escorted NPCs while not
//          in combat.  If a group of NPCs is being escorted, this distance is measured
//          from the group center point.
//      PriorityTargetId [optional; Default: none]
//          Any time a priority target is within the fighting area of the escort, it will
//          be dispatched first.  After all priority targets have been eliminated, the
//          behavior will go back to protecting the escorted units.
//      SearchForNpcsRadius [optional; Default: 75.0]
//          [on the closed range: [3.0..100.0]
//          This is the distance from the current position that the toon will look for
//          StartNpcIds or EscortNpcIds.
//
// BEHAVIOR EXTENSION ELEMENTS (goes between <CustomBehavior ...> and </CustomBehavior> tags)
// See the "Examples" section for typical usage.
//      SearchPath [optional; Default: Toon's current position when the behavior is started]
//          Comprised of a set of Hotspots, this element defines a _circular_ path
//          that the toon will traverse to locate the StartNpcIdN or EscortNpcIdN.
//          If no SearchPath is specified, the toon will wait at the current position
//          for the StartNpcIdN or EscortNpcIdN to arrive.
//
// THINGS TO KNOW:
// * The behavior lowers the "Pull Distance" while executing to avoid pulling mobs unnecessarily
// * All looting and harvesting is turned off while the escort is in progress.
//
#endregion


#region FAQs
// * Why the distinction between StartNpcIdN and EscortNpcIdN?
//      StartNpcIdN must be interacted via a gossip dialog to initiate the escort.
//      EscortNpcIdN represents a toon who's presence is sufficient to conduct
//      an escort.
//      Game mechanics are usually such that after the gossip completes with the
//      StartNpcIdN, an second instance of the gossip NPC is produced, and this
//      instanced-version is the NPC that should be followed (via EscortNpcIdN).
//      Some Escorts you just search for EscortNpcIdN and help them home
//      without chatting.
//
// * What happens if the toon gets in a fight but the NPCs keep going?
//      The toon will try to drag the mob to the NPC and pull the NPC into combat
//      also. This usually makes the NPC stop moving, and assist with the battle.
//
// * What happens when an Escorted NPC dies?
//      If there is more than one NPC being escorted, the behavior will continue
//      until all NPCs are dead.  Once the last NPC dies, the behavior will return
//      to its starting point and try again, unless EscortCompleteWhen is set to
//      QuestCompleteOrFails.
//
// * What happens if the Quest fails?
//      Normally, the behavior returns to its starting point to try again.  However,
//      if EscortCompleteWhen is set to QuestCompleteOrFails, the behavior will simply
//      terminate.
//
// * The quest failed, how do I arrange to abandon it, and go pick it up again?
//      Set the EscortCompleteWhen attribute to QuestCompleteOrFails.  In the profile,
//      check for quest failure, and abandon and pick the quest up again, before
//      re-conducting the behavior.
//
// * My toon is running to the mob to kill it, then running back to the escort.
//      The behavior's value of EscortMaxFightDistance is too low for the quest.
//      The profile needs to adjust this value accordingly.
//
// * My toon has an appropriate mob selected, but it won't engage it to kill
//      Some Combat Routines have bugs, and refuse to attack an appropriate target
//      if it is "too far away".  Adjust the value of CombatMaxEngagementRangeDistance
//      accordingly.
//
// * As soon as I gossip to start the escort, it tells me the Escort failed
//      The profile has selected improper values for the StartEscortGossipOptions
//      argument.
//
#endregion


#region Examples
// "Nefereset Prison" (http://wowhead.com/quest=27707)
// This used to be an escort quest, but has since been 'dumbed down' into a defend the NPC quest.
// The EscortGroup behavior is still very appropriate for defending stationary NPCs.
//      <CustomBehavior File="EscortGroup"  QuestId="27707"
//			StartNpcId="46425" StartEscortGossipOptions="1" EscortNpcId="46425"
//			EscortMaxFightDistance="30" EscortMaxFollowDistance="25" />
//
// "No Place To Run" (http://wowhead.com/quest=12261)
// Go to a particular spot and use the Destructive Wards (http://wowhead.com/item=37445)
// to spawn the Destrutive Ward (http://wowhead.com/npc=27430).  Defend the spawned
// Destructive Ward from the mobs that are attacking it, until the Ward charges to full.
//      <While Condition="!IsQuestCompleted(12261)" >
//          <UseItem QuestName="No Place to Run" QuestId="12261" ItemId="37445"
//              X="4384.706" Y="1305.638" Z="150.4314" />
//          <CustomBehavior File="EscortGroup" QuestId="12261" EscortMaxFightDistance="15"
//              SearchForNpcsRadius="15" EscortNpcId1="27430" EscortCompleteWhen="QuestCompleteOrFails" />
//      </While>
//
// "Reunited" (http://wowhead.com/quest=31091).
// A simple follow-and-defend quest.
// The quest requires interacting with an NPC (StartNpcId) via a gossip, then the gossip NPC
// is immediately replaced with an instanced-version which we need to escort (EscortNpcId).
//      <CustomBehavior File="EscortGroup"
//          QuestId="31091" EscortCompleteWhen="QuestObjectiveComplete" QuestObjectiveIndex="1"
//          StartNpcId="63876" StartEscortGossipOptions="1" EscortNpcId="64013" />
//
// "Students No More" (http://wowhead.com/quest=30625)
// Searchs for the students (EscortNpcIdN), then follows them to kill 4 elite mobs and some trash.
// The SearchPath is used to initially locate the students.
//      <CustomBehavior File="EscortGroup" QuestId="30625"
//          EscortNpcId1="59839" EscortNpcId2="59840" EscortNpcId3="59841" EscortNpcId4="59842" EscortNpcId5="59843">
//          <SearchPath>
//              <Hotspot X="-244.1373" Y="2327.563" Z="137.9225" />
//              <Hotspot X="-271.8149" Y="2286.35" 	Z="119.61" />
//              <Hotspot X="-278.60"   Y="2280.82"  Z="116.73" />
//              <Hotspot X="-377.7986" Y="2311.363" Z="117.677" />
//              <Hotspot X="-422.3317" Y="2303.213" Z="133.0315" />
//              <Hotspot X="-273.74"   Y="2351.56"  Z="126.98" />
//          </SearchPath>
//      </CustomBehavior>
//
// "The Burlap Trail: To Burlap Waystation" (http://wowhead.com/quest=30592)
// A simple "assist the NPCs home" quest.  Note there are three versions of the
// Grummle Trail Guide (the party's tank), and you don't know which version you
// will get for a particular party, so we include them all as EscortNpcIdN.
//      <CustomBehavior File="EscortGroup"  QuestId="30592"
//                      EscortNpcId1="59556" EscortNpcId2="59593" EscortNpcId3="59578"
//                      EscortNpcId4="59526" EscortNpcId5="59527"
//                      EscortMaxFightDistance="41" EscortMaxFollowDistance="25" >
//          <SearchPath>
//              <Hotspot X="2979.028" Y="1154.199" Z="627.852" />
//              <Hotspot X="2915.427" Y="1162.269" Z="616.4099" />
//              <Hotspot X="2918.999" Y="1244.736" Z="635.1276" />
//              <Hotspot X="2880.712" Y="1333.895" Z="639.9879" />
//              <Hotspot X="2859.79" Y="1421.056" Z="641.6649" />
//              <Hotspot X="2836.308" Y="1484.419" Z="641.7247" />
//              <Hotspot X="2848.853" Y="1394.382" Z="645.6835" />
//          </SearchPath>
//      </CustomBehavior>
//
#endregion


#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

using Bots.Grind;
using Bots.Quest.QuestOrder;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
#endregion


namespace Honorbuddy.Quest_Behaviors.EscortGroup
{
    [CustomBehaviorFileName(@"EscortGroup")]
    public class EscortGroup : QuestBehaviorBase
    {
        #region Constructor and Argument Processing
        public enum EscortCompleteWhenType
        {
            DestinationReached,
            QuestComplete,
            QuestCompleteOrFails,
            QuestObjectiveComplete,
        }

        public EscortGroup(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // Parameters dealing with 'starting' the behavior...
                StartEventGossipOptions = GetAttributeAsArray<int>("StartEscortGossipOptions", false, new ConstrainTo.Domain<int>(1, 10), null, null);
                StartNpcIds = GetNumberedAttributesAsArray<int>("StartNpcId", 0, ConstrainAs.MobId, null);

                // Parameters dealing with the Escorted Npcs...
                EscortNpcIds = GetNumberedAttributesAsArray<int>("EscortNpcId", 1, ConstrainAs.MobId, null);
                EscortCountMax = GetAttributeAsNullable<int>("EscortCountMax", false, new ConstrainTo.Domain<int>(1, 100), null) ?? 100;

                // Parameters dealing with when the task is 'done'...
                EscortCompleteWhen = GetAttributeAsNullable<EscortCompleteWhenType>("EscortCompleteWhen", false, null, null) ?? EscortCompleteWhenType.QuestComplete;
                EscortCompleteLocation = GetAttributeAsNullable<Vector3>("EscortComplete", false, ConstrainAs.Vector3NonEmpty, null) ?? Vector3.Zero;

                // Tunables...
                CombatMaxEngagementRangeDistance = GetAttributeAsNullable<double>("CombatMaxEngagementRangeDistance", false, new ConstrainTo.Domain<double>(1.0, 40.0), null) ?? 23.0;
                DebugReportUnitsOutOfRange = GetAttributeAsNullable<bool>("DebugReportUnitsOutOfRange", false, null, null) ?? false;
                EscortCompleteMaxRange = GetAttributeAsNullable<double>("EscortCompleteMaxRange", false, new ConstrainTo.Domain<double>(1.0, 100.0), null) ?? 10.0;
                EscortMaxFightDistance = GetAttributeAsNullable<double>("EscortMaxFightDistance", false, new ConstrainTo.Domain<double>(5.0, 100.0), null) ?? 27.0;
                EscortMaxFollowDistance = GetAttributeAsNullable<double>("EscortMaxFollowDistance", false, new ConstrainTo.Domain<double>(3.0, 100.0), null) ?? 15.0;
                PriorityTargetIds = GetNumberedAttributesAsArray<int>("PriorityTargetId", 0, ConstrainAs.MobId, null);
                SearchForNpcsRadius = GetAttributeAsNullable<double>("SearchForNpcsRadius", false, new ConstrainTo.Domain<double>(1.0, 100.0), null) ?? 75.0;

                // Semantic coherency / covariant dependency checks --
                if ((StartEventGossipOptions.Count() != 0) && (StartNpcIds.Count() == 0))
                {
                    QBCLog.Error("If StartEscortGossipOptions are specified, you must also specify one or more StartNpcIdN");
                    IsAttributeProblem = true;
                }

                if (EscortMaxFightDistance < EscortMaxFollowDistance)
                {
                    QBCLog.Error("EscortedNpcsMaxCombatDistance({0}) must be greater than or equal to EscortedNpcsMaxNoCombatDistance({1})",
                        EscortMaxFightDistance, EscortMaxFollowDistance);
                    IsAttributeProblem = true;
                }

                if ((EscortCompleteWhen == EscortCompleteWhenType.DestinationReached) && (EscortCompleteLocation == Vector3.Zero))
                {
                    QBCLog.Error("With a EscortCompleteWhen argument of DestinationReached, you must specify EscortCompleteX/EscortCompleteY/EscortCompleteZ arguments");
                    IsAttributeProblem = true;
                }

                if ((EscortCompleteWhen == EscortCompleteWhenType.QuestComplete) && (QuestId == 0))
                {
                    QBCLog.Error("With a EscortCompleteWhen argument of QuestComplete, you must specify a QuestId argument");
                    IsAttributeProblem = true;
                }

                if ((QuestId == 0) && (EscortCompleteWhen != EscortCompleteWhenType.DestinationReached))
                {
                    QBCLog.Error("When no QuestId is specified, EscortCompleteWhen must be DestinationReached");
                    IsAttributeProblem = true;
                }

                if ((EscortCompleteWhen == EscortCompleteWhenType.QuestObjectiveComplete)
                    && ((QuestId == 0) || (QuestObjectiveIndex == 0)))
                {
                    QBCLog.Error("With an EscortCompleteWhen argument of QuestObjectiveComplete, you must specify both QuestId and QuestObjectiveIndex arguments");
                    IsAttributeProblem = true;
                }

                if ((QuestObjectiveIndex != 0) && (EscortCompleteWhen != EscortCompleteWhenType.QuestObjectiveComplete))
                {
                    QBCLog.Error("The QuestObjectiveIndex argument should not be specified unless EscortCompleteWhen is QuestObjectiveComplete");
                    IsAttributeProblem = true;
                }

                if (StartEventGossipOptions.Count() == 0)
                    StartEventGossipOptions = new int[] { 0 };

                for (int i = 0; i < StartEventGossipOptions.Length; ++i)
                    StartEventGossipOptions[i] -= 1;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }


        // Variables for Attributes provided by caller
        public double CombatMaxEngagementRangeDistance { get; private set; }
        public bool DebugReportUnitsOutOfRange { get; private set; }

        public EscortCompleteWhenType EscortCompleteWhen { get; private set; }
        public Vector3 EscortCompleteLocation { get; private set; }
        public double EscortCompleteMaxRange { get; private set; }

        public int EscortCountMax { get; private set; }
        public int[] EscortNpcIds { get; private set; }
        public double EscortMaxFightDistance { get; private set; }
        public double EscortMaxFollowDistance { get; private set; }

        public int[] PriorityTargetIds { get; private set; }
        public double SearchForNpcsRadius { get; private set; }
        public int[] StartEventGossipOptions { get; private set; }
        public int[] StartNpcIds { get; private set; }


        // DON'T EDIT THIS--it is auto-populated by Git
        protected override string GitId => "$Id$";
        #endregion


        #region Private and Convenience variables
        private delegate Vector3 LocationDelegate(object context);
        private delegate string MessageDelegate(object context);
        private delegate WoWUnit WoWUnitDelegate(object context);

        private enum BehaviorStateType
        {
            InitialState,
            SearchingForEscortUnits,
            InteractingToStart,
            IdentifySpecificUnitsToEscort,
            Escorting,
            CheckDone,
        }

        private class MovementState
        {
            public bool IsMoveInProgress { get; set; }
        }

        private BehaviorStateType BehaviorState
        {
            get { return _behaviorState; }
            set
            {
                // For DEBUGGING...
                if (_behaviorState != value)
                { QBCLog.DeveloperInfo("Behavior State: {0}", value); }
                _behaviorState = value;
            }
        }
        private readonly TimeSpan _duration_BlacklistGossip = TimeSpan.FromSeconds(120);
        private readonly TimeSpan _delay_GossipDialogThrottle = TimeSpan.FromMilliseconds(StyxWoW.Random.Next(800, 1700));
        private List<WoWUnit> EscortedGroup { get; set; }
        private readonly TimeSpan _lagDuration = TimeSpan.FromMilliseconds((StyxWoW.WoWClient.Latency * 2) + 150);
        private WoWUnit SelectedTarget { get; set; }

        private BehaviorStateType _behaviorState = BehaviorStateType.CheckDone;
        private Composite _behaviorTreeHook_Main;
        private readonly LocalBlacklist _gossipBlacklist = new LocalBlacklist(TimeSpan.FromSeconds(30));
        private int _gossipOptionIndex;
        private readonly MovementState _movementStateForCombat = new MovementState();
        private readonly MovementState _movementStateForNonCombat = new MovementState();
        private Queue<Vector3> _searchPath;
        private Vector3 _toonStartingPosition = Vector3.Zero;
        #endregion


        #region Overrides of CustomForcedBehavior

        protected override void EvaluateUsage_DeprecatedAttributes(XElement xElement)
        {
            //// EXAMPLE:
            //UsageCheck_DeprecatedAttribute(xElement,
            //    Args.Keys.Contains("Nav"),
            //    "Nav",
            //    context => string.Format("Automatically converted Nav=\"{0}\" attribute into MovementBy=\"{1}\"."
            //                              + "  Please update profile to use MovementBy, instead.",
            //                              Args["Nav"], MovementBy));
        }

        protected override void EvaluateUsage_SemanticCoherency(XElement xElement)
        {
            //// EXAMPLE:
            //UsageCheck_SemanticCoherency(xElement,
            //    (!MobIds.Any() && !FactionIds.Any()),
            //    context => "You must specify one or more MobIdN, one or more FactionIdN, or both.");
            //
            //const double rangeEpsilon = 3.0;
            //UsageCheck_SemanticCoherency(xElement,
            //    ((RangeMax - RangeMin) < rangeEpsilon),
            //    context => string.Format("Range({0}) must be at least {1} greater than MinRange({2}).",
            //                  RangeMax, rangeEpsilon, RangeMin));
        }

        protected override Composite CreateMainBehavior()
        {
            return _behaviorTreeHook_Main ?? (_behaviorTreeHook_Main = CreateMainLogic());
        }

        public override void OnStart()
        {
            // Let QuestBehaviorBase do basic initialization of the behavior, deal with bad or deprecated attributes,
            // capture configuration state, install BT hooks, etc.  This will also update the goal text.
            var isBehaviorShouldRun = OnStart_QuestBehaviorCore();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (isBehaviorShouldRun)
            {
                _searchPath = ParsePath("SearchPath");

                // Disable any settings that may interfere with the escort --
                // When we escort, we don't want to be distracted by other things.
                LevelBot.BehaviorFlags &= ~(BehaviorFlags.Vendor | BehaviorFlags.FlightPath | BehaviorFlags.Loot);

                // Disable pulling if targets are selected explicitly
                if (PriorityTargetIds.Any())
                    LevelBot.BehaviorFlags &= ~BehaviorFlags.Pull;

                // If search path not provided, use our current location...
                if (!_searchPath.Any())
                    _searchPath.Enqueue(Me.Location);

                _toonStartingPosition = Me.Location;

                BehaviorState = BehaviorStateType.InitialState;

                this.UpdateGoalText(QuestId, "Looting and Harvesting are disabled while Escort in progress");
            }
        }

        protected override void TargetFilter_IncludeTargets(List<WoWObject> incomingWowObjects, HashSet<WoWObject> outgoingWowObjects)
        {
            var mobs =
                ObjectManager.GetObjectsOfType<WoWUnit>()
                .Where(
                    u =>
                    u.IsAlive && u.Combat && u.CurrentTarget != null &&
                    EscortNpcIds.Contains((int)u.CurrentTarget.Entry));

            foreach (var m in mobs)
                outgoingWowObjects.Add(m);
        }
        #endregion


        #region Main Behavior
        protected override Composite CreateBehavior_CombatMain()
        {
            return new Decorator(context => (BehaviorState == BehaviorStateType.Escorting) && IsEscortedGroupViable(EscortedGroup),
                new PrioritySelector(

                    // Toon should *never* drift more than EscortMaxFightDistance from nearest escorted unit...
                    UtilityBehavior_MoveTo(
                        _movementStateForCombat,
                        context => EscortedGroup.All(u => u.Distance > EscortMaxFightDistance),
                        context => EscortedGroup.Any(u => u.Distance < EscortMaxFollowDistance),
                        context => FindGroupCenterPoint(EscortedGroup),
                        context => "nearest escorted unit"),

                    // Deal with priority targets...
                    new PrioritySelector(priorityUnitContext => FindPriorityTargets(EscortedGroup).OrderBy(u => u.DistanceSqr).FirstOrDefault(),
                        // If the SelectedTarget is not a priority target, switch it...
                        new Decorator(priorityUnitContext => (priorityUnitContext != null)
                                                            && (!Query.IsViableForFighting(SelectedTarget)
                                                                || !PriorityTargetIds.Contains((int)SelectedTarget.Entry)),
                            new Action(priorityUnitContext =>
                            {
                                SelectedTarget = (WoWUnit)priorityUnitContext;

                                QBCLog.Info("Switching to priority target {0}", SelectedTarget.SafeName);
                                BotPoi.Current = new BotPoi(SelectedTarget, PoiType.Kill, QuestOrder.Instance.NavType);
                                SelectedTarget.Target();
                                return RunStatus.Failure; // fall through
                            }))
                    ),

                    // NB: Make certain Honorbuddy stays focused on our kill target...
                    // This is required because during an escort, we can be in combat with no units attacking us.
                    // If this happens, HB will just "stand around" while the escorted units get pounded on.
                    // We must assure the intended target gets attacked, even if HB thinks differently.
                    new Decorator(context => Query.IsViableForFighting(SelectedTarget),
                        new PrioritySelector(
                            new Decorator(context => BotPoi.Current.Type != PoiType.Kill,
                                new Action(context =>
                                {
                                    BotPoi.Current = new BotPoi(SelectedTarget, PoiType.Kill, QuestOrder.Instance.NavType);
                                    SelectedTarget.Target();
                                    return RunStatus.Failure;   // fall through
                                })),

                            // We have a target, if not in combat get it started...
                            // HB is slow to engage mobs via POI.
                            new Decorator(context => !Me.Combat,
                                UtilityBehavior_SpankMob(context => SelectedTarget))
                        )),

                    // If an escorted group member still in combat, find next target...
                    new Decorator(context => !Query.IsViableForFighting(SelectedTarget) && IsAnyBeingTargeted(EscortedGroup),
                        new Action(context =>
                        {
                            // Debug: Report out-of-range hostiles (Useful for profile development)
                            if (DebugReportUnitsOutOfRange)
                            {
                                var outOfRangeUnitsQuery = FindUnitsOutOfRange(EscortedGroup);
                                var outOfRangeUnits = outOfRangeUnitsQuery as IList<Tuple<WoWUnit, double>> ?? outOfRangeUnitsQuery.ToList();

                                if (outOfRangeUnits.Any())
                                {
                                    QBCLog.Warning("Some units exceed the EscortMaxFightDistance range ({0} yard): {1}",
                                        EscortMaxFightDistance,
                                        string.Join(", ", outOfRangeUnits.Select(u => string.Format("{0}({1:F1})",
                                                        u.Item1.SafeName, u.Item2))));
                                }
                            }

                            SelectedTarget = ChooseBestTarget(EscortedGroup);
                            return RunStatus.Failure;   // Don't starve normal 'combat routine' actions
                        }))
                ));
        }


        protected override Composite CreateBehavior_CombatOnly()
        {
            return new PrioritySelector(
            // empty
            );
        }


        protected override Composite CreateBehavior_DeathMain()
        {
            // If toon dies, we need to restart behavior
            return new Decorator(context => (Me.IsDead || Me.IsGhost) && (BehaviorState != BehaviorStateType.CheckDone),
                new Action(context => { BehaviorState = BehaviorStateType.CheckDone; }));
        }


        // NB: Due to the complexity, this behavior is 'state' based.  All necessary actions are
        // conducted in the current state.  If the current state is no longer valid, then a state change
        // is effected.  Ths entry state is "InitialState".
        private Composite CreateMainLogic()
        {
            // Let other behaviors deal with toon death and path back to corpse...
            return new PrioritySelector(
                //FOR DEBUG:
                // new Action(escortedUnitsContext => { LogInfo("Current State: {0}", _behaviorState); return RunStatus.Failure; }),

                new Decorator(context => IsDone,
                    new Action(context => { QBCLog.Info("Finished"); })),

                new Switch<BehaviorStateType>(escortedUnitsContext => BehaviorState,
                    new Action(context =>   // default case
                    {
                        var message = string.Format("BehaviorState({0}) is unhandled", BehaviorState);
                        QBCLog.MaintenanceError(message);
                        TreeRoot.Stop();
                        BehaviorDone(message);
                    }),

            #region State: InitialState
                new SwitchArgument<BehaviorStateType>(BehaviorStateType.InitialState,
                        new PrioritySelector(
                            UtilityBehavior_MoveTo(
                                _movementStateForNonCombat,
                                context => true,
                                context => false,
                                context => _toonStartingPosition,
                                context => "start location"),

                            // Start at nearest point in the search path...
                            new Action(context =>
                            {
                                var nearestPoint = _searchPath.OrderBy(p => Me.Location.Distance(p)).FirstOrDefault();
                                while (_searchPath.Peek() != nearestPoint)
                                { Utility_RotatePath(_searchPath); }

                                EscortedGroup = new List<WoWUnit>();
                                BehaviorState = BehaviorStateType.SearchingForEscortUnits;
                            })
                        )),
            #endregion


            #region State: SearchingForEscortUnits
                new SwitchArgument<BehaviorStateType>(BehaviorStateType.SearchingForEscortUnits,
                    new PrioritySelector(
                        // If Start NPCs specified, move to them when found...
                        new Decorator(context => StartNpcIds.Count() > 0,
                            new PrioritySelector(startUnitsContext => FindEscortedUnits(StartNpcIds, SearchForNpcsRadius),
                                new Decorator(startUnitsContext => ((IEnumerable<WoWUnit>)startUnitsContext).Any(),
                                    new Action(startUnitsContext => BehaviorState = BehaviorStateType.InteractingToStart)
                                ))),

                        // If only Escort NPCs specified, move to the EscortedNpcs when found...
                        new Decorator(context => (StartNpcIds.Count() <= 0),
                            new PrioritySelector(startUnitsContext => FindEscortedUnits(EscortNpcIds, SearchForNpcsRadius),
                                new Decorator(startUnitsContext => ((IEnumerable<WoWUnit>)startUnitsContext).Any(),
                                    new Action(startUnitsContext => { BehaviorState = BehaviorStateType.IdentifySpecificUnitsToEscort; })
                                ))),

                        // Mount up to start searching...
                        // NB: we can't push this into the UtilityBehavior_MoveTo() routine, because our waypoints
                        // may be closely spaced.  Its the fact that we're "searching" that determines we should mount,
                        // not the distance of movement involved.
                        new Decorator(context => !Me.Mounted && Mount.CanMount(),
                            new ActionRunCoroutine(context => CommonCoroutines.SummonGroundMount())),

                        // If we've reached the next point in the search path, and there is more than one, update path...
                        new Decorator(context => Navigator.AtLocation(_searchPath.Peek()) && _searchPath.Count() > 1,
                            new Action(context => { Utility_RotatePath(_searchPath); return RunStatus.Failure; })),

                        // Move to next search waypoint as needed...
                        UtilityBehavior_MoveTo(
                            _movementStateForNonCombat,
                            context => true,
                            context => false,
                            context => _searchPath.Peek(),
                            context => "next search waypoint"),

                        // If no search path, or only one point, just sit at current position and await
                        // for NPCs to arrive...
                        new Decorator(context => _searchPath.Count() <= 1,
                            new CompositeThrottleContinue(TimeSpan.FromSeconds(60),
                                new Action(context => { QBCLog.Info("Waiting for NPCs to arrive"); })))
                        )),
            #endregion


            #region State: InteractingToStart
                    // NB:some escorts depop the interaction NPC and immediately replace with the escort-instance version
                    // after selecting the appropriate gossip options.  Do NOT be tempted to check for presence of
                    // correct NPC while in this state--it will hang the behavior tree if it is immediately replaced on gossip.
                    new SwitchArgument<BehaviorStateType>(BehaviorStateType.InteractingToStart,
                        new PrioritySelector(
                            // If a mob is targeting us, deal with it immediately, so our interact actions won't be interrupted...
                            // NB: This can happen if we 'drag mobs' behind us on the way to meeting the escorted units.
                            UtilityBehavior_SpankMobTargetingUs(),

                            // If no interaction required to start escort, then proceed escorting
                            new Decorator(context => StartNpcIds.Count() <= 0,
                                new Action(context => { BehaviorState = BehaviorStateType.IdentifySpecificUnitsToEscort; })),

                            // Continue with interaction
                            UtilityBehavior_GossipToStartEvent(),

                            new Action(context =>
                            {
                                if (GossipFrame.Instance != null)
                                    GossipFrame.Instance.Close();
                                Me.ClearTarget();
                                BehaviorState = BehaviorStateType.IdentifySpecificUnitsToEscort;
                            })
                        )),
            #endregion


            #region State: Identify units to be escorted
                    new SwitchArgument<BehaviorStateType>(BehaviorStateType.IdentifySpecificUnitsToEscort,
                        new PrioritySelector(
                            // If a mob is targeting us, deal with it immediately, so our subsequent actions won't be interrupted...
                            // NB: This can happen if we 'drag mobs' behind us on the way to meeting the escorted units.
                            UtilityBehavior_SpankMobTargetingUs(),

                            // Find a candidate group...
                            new Decorator(context => !IsEscortedGroupViable(EscortedGroup),
                                new Action(context =>
                                {
                                    EscortedGroup = new List<WoWUnit>(FindEscortedUnits(EscortNpcIds, SearchForNpcsRadius));

                                    if (!IsEscortedGroupViable(EscortedGroup))
                                        BehaviorState = BehaviorStateType.SearchingForEscortUnits;
                                })),

                            // Move to the group...
                            UtilityBehavior_MoveTo(
                                _movementStateForNonCombat,
                                context => EscortedGroup.All(u => u.Distance > EscortMaxFightDistance),
                                context => EscortedGroup.Any(u => u.Distance < EscortMaxFollowDistance),
                                context => FindGroupCenterPoint(EscortedGroup),
                                context => "nearest escorted unit"),

                            // "Lock in" the units we're going to escort, and start escorting
                            new Action(context =>
                            {
                                Me.ClearTarget();
                                EscortedGroup = new List<WoWUnit>(FindEscortedUnits(EscortNpcIds, SearchForNpcsRadius));
                                QBCLog.Info("Escorting {0} units: {1}",
                                    EscortedGroup.Count(),
                                    string.Join(", ", EscortedGroup.Select(u => string.Format("{0} (dist: {1:F1})", u.SafeName, u.Distance))));
                                BehaviorState = BehaviorStateType.Escorting;
                            })
                        )),
            #endregion


            #region State: Escorting
                    new SwitchArgument<BehaviorStateType>(BehaviorStateType.Escorting,
                        new PrioritySelector(
                            // Escort complete or failed?
                            new Decorator(context => (IsEscortComplete(EscortedGroup) || IsEscortFailed(EscortedGroup)),
                                new Action(context => { BehaviorState = BehaviorStateType.CheckDone; })),

                            new Decorator(context => !Me.Combat,
                                UtilityBehavior_MoveTo(
                                    _movementStateForNonCombat,
                                    context => EscortedGroup.Any(u => !u.IsFacing(Me)) && EscortedGroup.All(u => u.IsMoving),
                                    context => EscortedGroup.All(u => u.IsFacing(Me) && (u.Distance > EscortMaxFollowDistance)),
                                    context => FindPositionToEscort(EscortedGroup),
                                    context => "escort"
                                    ))
                        )),
            #endregion


            #region State: CheckDone
                    new SwitchArgument<BehaviorStateType>(BehaviorStateType.CheckDone,
                        new PrioritySelector(
                            new Decorator(context => !IsEscortComplete(EscortedGroup) && !IsEscortFailed(EscortedGroup),
                                new Action(context => { BehaviorState = BehaviorStateType.Escorting; })),

                            new Action(context =>
                            {
                                if (IsEscortFailed(EscortedGroup))
                                    QBCLog.Warning("Looks like we've failed the escort.");

                                if (IsEscortComplete(EscortedGroup))
                                {
                                    var message = string.Format("Behavior complete (EscortCompleteWhen=\"{0}\")", EscortCompleteWhen);
                                    QBCLog.Info(message);
                                    BehaviorDone(message);
                                }
                                else
                                {
                                    QBCLog.Info("Returning to start to re-do.");
                                    BehaviorState = BehaviorStateType.InitialState;
                                }
                            })))
            #endregion
                    ));
        }
        #endregion


        #region Helpers
        // Get the weakest mob attacking our weakest escorted unit...
        private WoWUnit ChooseBestTarget(List<WoWUnit> escortedUnits)
        {
            if (!IsEscortedGroupViable(escortedUnits))
                return null;

            var hostilesQuery = FindAllTargets(escortedUnits);
            var hostiles = hostilesQuery as IList<WoWUnit> ?? hostilesQuery.ToList();

            return
               (from unit in hostiles
                let attackedEscortUnit = unit.CurrentTarget
                // The +1 term doesn't change relative weighting, and prevents division by zero in evaluation equation
                let unitCountAttackingEscortUnit = hostiles.Count(u => attackedEscortUnit == u.CurrentTarget) + 1
                orderby // evaluation equation:
                    attackedEscortUnit.HealthPercent / unitCountAttackingEscortUnit // prefer low health escorted that are surrounded
                    + unit.HealthPercent                                    // prefer weaker enemies
                    + unit.Location.Distance(attackedEscortUnit.Location)   // prefer nearby mobs
                    + (unit.Elite ? 1000 : 1)                               // prefer non-elite mobs
                    + (unit.IsTargetingMeOrPet ? 100 : 1)                   // prefer targets attacking escorted units (instead of myself/pet)
                select unit
                ).FirstOrDefault();
        }


        /// <summary>Finds all enemies attacking ESCORTEDUNITS, or the myself or pet</summary>
        private IEnumerable<WoWUnit> FindAllTargets(IEnumerable<WoWUnit> escortedUnits)
        {
            // NB: Some combat AoE effects will snag 'neutral' targets, so the test is intentionally
            // for !IsFriendly, instead of IsHostile.
            return
                from unit in ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                where
                    Query.IsViable(unit)
                    && !unit.IsFriendly
                    && !unit.IsPlayer
                    && unit.IsAlive
                    && (unit.IsTargetingMeOrPet || unit.IsTargetingAnyMinion || IsTargettingGroupMember(unit, escortedUnits))
                    && !Blacklist.Contains(unit, BlacklistFlags.Combat)
                select unit;
        }


        private IEnumerable<WoWUnit> FindEscortedUnits(IEnumerable<int> unitIds, double searchRadius)
        {
            var searchRadiusSqr = searchRadius * searchRadius;

            return
               (from unit in FindUnitsFromIds(unitIds)
                where
                    unit.IsAlive
                    && (unit.DistanceSqr < searchRadiusSqr)
                    && (!FindPlayersNearby(unit.Location, NonCompeteDistance).Any())
                let ownedBy = unit.OwnedByRoot
                // skip NPCs that are escorted by other players.
                where ownedBy == null || ownedBy.Guid == Me.Guid || Me.GroupInfo.PartyMemberGuids.Contains(ownedBy.Guid)
                orderby unit.DistanceSqr
                select unit)
                .Take(EscortCountMax);
        }


        // Returns group center point or, Vector3.Zero if group is empty
        private Vector3 FindGroupCenterPoint(IEnumerable<WoWUnit> groupMembers)
        {
            var groupMemberCount = 0;
            var centerPoint = new Vector3();

            foreach (var wowUnit in groupMembers)
            {
                centerPoint.X += wowUnit.Location.X;
                centerPoint.Y += wowUnit.Location.Y;
                centerPoint.Z += wowUnit.Location.Z;
                ++groupMemberCount;
            }

            if (groupMemberCount > 0)
            {
                centerPoint.X /= groupMemberCount;
                centerPoint.Y /= groupMemberCount;
                centerPoint.Z /= groupMemberCount;

                FindVector3Height(ref centerPoint);
                return centerPoint;
            }

            return Me.Location;
        }


        // 25Feb2013-12:50UTC chinajade
        private IEnumerable<WoWUnit> FindNonFriendlyTargetingMeOrPet()
        {
            return
                from unit in ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                where
                    Query.IsViableForFighting(unit)
                    && unit.IsTargetingMeOrPet
                select unit;
        }


        // 25Feb2013-12:50UTC chinajade
        private IEnumerable<WoWPlayer> FindPlayersNearby(Vector3 location, double radius)
        {
            return from player in ObjectManager.GetObjectsOfType<WoWPlayer>(true, false)
                   where Query.IsViable(player)
                         && player.IsAlive
                         && player.Location.Distance(location) < radius
                   select player;
        }

        private Vector3 FindPositionToEscort(IEnumerable<WoWUnit> escortedUnitsEnumerable)
        {
            var escortedUnits = escortedUnitsEnumerable as IList<WoWUnit> ?? escortedUnitsEnumerable.ToList();
            var groupCenterPoint = FindGroupCenterPoint(escortedUnits);

            // Find aggregate heading...
            double aggregateHeading = escortedUnits.Average(u => u.Rotation);
            var unitNearestGroupCenter = escortedUnits.OrderBy(u => u.Location.Distance(groupCenterPoint)).FirstOrDefault();

            if (unitNearestGroupCenter == null)
                return Vector3.Zero;

            var centerLocation = unitNearestGroupCenter.Location;

            var positionToEscort = centerLocation.RayCast((float)aggregateHeading, (float)EscortMaxFollowDistance);

            // set the 'positionToEscort' to 'hitPoint' if 'positionToEscort' is off the mesh, on another level or obstructed.
            Vector3 hitPoint;
            var meshIsObstructed = MeshTraceline(centerLocation, positionToEscort, out hitPoint);
            if (meshIsObstructed.HasValue && meshIsObstructed.Value)
                positionToEscort = hitPoint;

            // if FindVector3Height returns false then no mesh was found at 'positionToEscort' location
            // so the centerLocation is returned instead.
            return !FindVector3Height(ref positionToEscort)
                ? centerLocation
                : positionToEscort;
        }


        private IEnumerable<WoWUnit> FindPriorityTargets(IEnumerable<WoWUnit> escortedGroup)
        {
            return
                from unit in FindUnitsFromIds(PriorityTargetIds)
                where
                    Query.IsViableForFighting(unit)
                    && escortedGroup.Any(g => g.Location.Distance(unit.Location) < EscortMaxFightDistance)
                select unit;
        }


        private IEnumerable<WoWUnit> FindUnitsFromIds(IEnumerable<int> unitIds)
        {
            return
                from unit in ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                where
                    Query.IsViable(unit)
                    && unitIds.Contains((int)unit.Entry)
                select unit;
        }


        /// <summary>list of tuples of all aggro'd hostiles not in EscortMaxFightDistance an any ESCORTEDUNITS.
        /// The tuple is composed of the unit that is out of range, along with their distance to the nearest
        /// escorted unit.</summary>
        private IEnumerable<Tuple<WoWUnit, double>> FindUnitsOutOfRange(List<WoWUnit> escortedUnits)
        {
            return
                from unit in FindAllTargets(escortedUnits)
                let minDistance = escortedUnits.Min(e => (double)e.Location.Distance(unit.Location))
                where minDistance > EscortMaxFightDistance
                select Tuple.Create(unit, minDistance);
        }


        private bool FindVector3Height(ref Vector3 Vector3)
        {
            return true;
            // TODO: FindHeights. Rewrite callers of this to use mesh sampling instead.
            //			var heights = Navigator.FindHeights(Vector3.X, Vector3.Y);
            //			if (heights == null || !heights.Any())
            //				return false;
            //			var tmpZ = Vector3.Z;
            //			// find the height that is nearest to current Vector3.Z value
            //			Vector3.Z = heights.OrderBy(h => Math.Abs(h - tmpZ)).FirstOrDefault();
            //			return true;
        }


        private bool IsAnyBeingTargeted(IEnumerable<WoWUnit> group)
        {
            return
                ObjectManager.GetObjectsOfType<WoWUnit>()
                .Any(u => IsTargettingGroupMember(u, group));
        }


        private bool IsEscortComplete(List<WoWUnit> escortedUnits)
        {
            switch (EscortCompleteWhen)
            {
                case EscortCompleteWhenType.DestinationReached:
                    return Me.Location.Distance(EscortCompleteLocation) <= EscortCompleteMaxRange;

                case EscortCompleteWhenType.QuestComplete:
                    {
                        var quest = Me.QuestLog.GetQuestById((uint)QuestId);
                        return (quest == null) || quest.IsCompleted;
                    }

                case EscortCompleteWhenType.QuestCompleteOrFails:
                    {
                        var quest = Me.QuestLog.GetQuestById((uint)QuestId);
                        return (quest == null) || quest.IsCompleted || IsEscortFailed(escortedUnits);
                    }

                case EscortCompleteWhenType.QuestObjectiveComplete:
                    return Me.IsQuestObjectiveComplete(QuestId, QuestObjectiveIndex);
            }

            QBCLog.MaintenanceError("EscortCompleteWhen({0}) state is unhandled", EscortCompleteWhen);
            TreeRoot.Stop();
            return true;
        }


        // Escort fails when 1) quest says so, or 2) there are no more units to escort
        private bool IsEscortFailed(List<WoWUnit> escortedUnits)
        {
            bool isFailed = !IsEscortedGroupViable(escortedUnits);

            if (QuestId > 0)
            {
                PlayerQuest quest = Me.QuestLog.GetQuestById((uint)QuestId);
                isFailed |= quest.IsFailed;
            }

            return isFailed;
        }


        /// <summary>
        /// Viable if we have members in the group, and at least one is alive...
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private bool IsEscortedGroupViable(List<WoWUnit> group)
        {
            Contract.Requires(group != null, (context) => "group may not be null");

            group.RemoveAll(u => !u.IsValid || !u.IsAlive);

            return group.Any(u => Query.IsViable(u) && u.IsAlive);
        }


        // returns true, if WOWUNIT is targeting any member of GROUP (or a group member's pet)
        private bool IsTargettingGroupMember(WoWUnit wowUnit, IEnumerable<WoWUnit> group)
        {
            Contract.Requires(group != null, (context) => "group may not be null");

            var currentTarget = wowUnit.CurrentTarget;

            if (currentTarget == null)
                return false;

            var currentTargetGuid = currentTarget.Guid;

            // NB: We can only check player pets... checking for NPC pets gives Honorbuddy heartburn
            return group.Any(m => (currentTargetGuid == m.Guid)
                            || (m.IsPlayer && m.GotAlivePet && (currentTargetGuid == m.Pet.Guid)));
        }

        /// <summary>
        /// This casts a walkable ray on the surface of the mesh from <c>Vector3Src</c> to <c>Vector3Dest</c> and
        /// return value indicates whether a wall (disjointed polygon edge) was encountered
        /// </summary>
        /// <param name="Vector3Src"></param>
        /// <param name="Vector3Dest"></param>
        /// <param name="hitLocation">
        /// The point where a wall (disjointed polygon edge) was encountered if any, otherwise Vector3.Zero.
        /// The hit calculation is done in 2d so the Z coord will not be accurate;
        ///  It is an interpolation between <c>Vector3Src</c>'s and <c>Vector3Dest</c>'s Z coords
        /// </param>
        /// <returns>Returns null if a result cannot be determined e.g <c>Vector3Dest</c> is not on mesh,
        ///  True if a wall (disjointed polygon edge) is encountered otherwise false</returns>
        private static bool? MeshTraceline(Vector3 Vector3Src, Vector3 Vector3Dest, out Vector3 hitLocation)
        {
#warning FIXME MeshTraceline
            //TODO: MeshTraceline. Caller of this can use mesh sampling.
            hitLocation = Vector3.Zero;
            return null;
            //			var meshNav = Navigator.NavigationProvider as MeshNavigator;
            //			// 99.999999 % of the time Navigator.NavigationProvider will be a MeshNavigator type or subtype -
            //			// but if it isn't then bail because another navigation system is being used.
            //			if (meshNav == null)
            //				return null;
            //
            //			var wowNav = meshNav.Nav;
            //			var detourPointSrc = NavHelper.ToNav(Vector3Src);
            //			var detourPointDest = NavHelper.ToNav(Vector3Dest);
            //
            //			// ensure tiles for start and end location are loaded. this does nothing if they're already loaded
            //			wowNav.LoadTile(TileIdentifier.GetByPosition(Vector3Src));
            //			wowNav.LoadTile(TileIdentifier.GetByPosition(Vector3Dest));
            //
            //			Vector3 nearestPolyPoint;
            //			PolygonReference polyRef;
            //			var status = wowNav.MeshQuery.FindNearestPolygon(
            //				detourPointSrc,
            //				wowNav.Extents,
            //				wowNav.QueryFilter.InternalFilter,
            //				out nearestPolyPoint,
            //				out polyRef);
            //			if (status.Failed || polyRef.Id == 0)
            //				return null;
            //
            //			PolygonReference[] raycastPolys;
            //			float rayHitDist;
            //			Vector3 rayHitNorml;
            //
            //			//  normalized distance (0 to 1.0) if there was a hit, otherwise float.MaxValue.
            //			status = wowNav.MeshQuery.Raycast(
            //				polyRef,
            //				detourPointSrc,
            //				detourPointDest,
            //				wowNav.QueryFilter.InternalFilter,
            //				500,
            //				out raycastPolys,
            //				out rayHitDist,
            //				out rayHitNorml);
            //
            //			if (status.Failed)
            //				return null;
            //
            //			// check if there's a hit
            //			if (rayHitDist < float.MaxValue)
            //			{
            //				// get Vector3Src to Vector3Dest vector
            //				var startToEndOffset = Vector3Dest - Vector3Src;
            //				// multiply segmentEndToNewPoint by rayHitDistance and add quanity to segmentEnd to get ray hit point.
            //				// N.B. the Z coord will be an interpolation between Vector3Src and Vector3Desc Z coords
            //				// because the hit calculation is done in 2d
            //				hitLocation = startToEndOffset * rayHitDist + Vector3Src;
            //				return true;
            //			}
            //			return false;
        }


        /// <summary>
        /// When STARTMOVINGWHEN is true, this behavior moves to LOCATIONDELEGATE.  When STOPMOVINGWHEN is true,
        /// or the toon is within PRECISIONDELEGATE of LOCATIONDELEGATE, the behavior ceases to issue move
        /// directives.  STOPMOVINGWHEN takes precedence over STARTMOVINGWHEN, if both are true.
        /// </summary>
        /// <param name="movementState"></param>
        /// <param name="startMovingWhen"></param>
        /// <param name="stopMovingWhen"></param>
        /// <param name="locationDelegate"></param>
        /// <param name="locationNameDelegate"></param>
        /// <returns>RunStatus.Success while movement is in progress; othwerise, RunStatus.Failure if no movement necessary</returns>
        private Composite UtilityBehavior_MoveTo(MovementState movementState,
                                                 CanRunDecoratorDelegate startMovingWhen,
                                                 CanRunDecoratorDelegate stopMovingWhen,
                                                 LocationDelegate locationDelegate,
                                                 MessageDelegate locationNameDelegate)
        {
            return new PrioritySelector(
                // Need to start moving?
                new Decorator(context => !movementState.IsMoveInProgress && startMovingWhen(context) && !stopMovingWhen(context)
                                            && !Navigator.AtLocation(locationDelegate(context)),
                    new Action(context => { movementState.IsMoveInProgress = true; })),

                // Headed somewhere?
                new Decorator(context => movementState.IsMoveInProgress,
                    new PrioritySelector(
                        // Did we arrive at destination?
                        new Decorator(context => Navigator.AtLocation(locationDelegate(context)),
                            new Action(context => { movementState.IsMoveInProgress = false; })),

                        // Did stop trigger activate?
                        new Decorator(context => stopMovingWhen(context),
                            new Action(context => { movementState.IsMoveInProgress = false; })),

                        // Notify user of progress...
                        new CompositeThrottle(TimeSpan.FromSeconds(1),
                            new Action(context =>
                            {
                                var locationName = locationNameDelegate(context) ?? locationDelegate(context).ToString();
                                QBCLog.Info("Moving to {0}", locationName);
                                return RunStatus.Failure; // fall through after notifying user
                            })),


                        // Conduct movement...
                        new Action(context =>
                        {
                            var destination = locationDelegate(context);

                            // Try to use Navigator to get there...
                            var moveResult = Navigator.MoveTo(destination);

                            // If Navigator fails, fall back to click-to-move...
                            if ((moveResult == MoveResult.Failed) || (moveResult == MoveResult.PathGenerationFailed))
                                WoWMovement.ClickToMove(destination);

                            return RunStatus.Success;
                        })
                    ))
            );
        }


        /// <returns>Returns RunStatus.Success if gossip in progress; otherwise, RunStatus.Failure if gossip complete or unnecessary</returns>
        private Composite UtilityBehavior_GossipToStartEvent()
        {
            return new PrioritySelector(gossipUnitContext => FindEscortedUnits(StartNpcIds, SearchForNpcsRadius).OrderBy(u => u.Distance).FirstOrDefault(),
                new Decorator(gossipUnitContext => (gossipUnitContext != null) && !_gossipBlacklist.Contains((WoWUnit)gossipUnitContext),
                    new PrioritySelector(
                        // If unit in line of sight, target it...
                        new Decorator(gossipUnitContext => (Me.CurrentTarget != (WoWUnit)gossipUnitContext)
                                                            && Query.IsInLineOfSight((WoWUnit)gossipUnitContext),
                            new Action(gossipUnitContext =>
                            {
                                ((WoWUnit)gossipUnitContext).Target();
                                return RunStatus.Failure;   // fall through
                            })),

                        // Move to closest unit...
                        UtilityBehavior_MoveTo(
                            _movementStateForNonCombat,
                            gossipUnitContext => true,
                            gossipUnitContext => false,
                            gossipUnitContext => ((WoWUnit)gossipUnitContext).Location,
                            gossipUnitContext => ((WoWUnit)gossipUnitContext).SafeName),

                        new Decorator(ctx => Me.Mounted, new ActionRunCoroutine(ctx => CommonCoroutines.LandAndDismount())),

                        // Interact with unit to open the Gossip dialog...
                        new Decorator(gossipUnitContext => (GossipFrame.Instance == null) || !GossipFrame.Instance.IsVisible,
                            new Sequence(
                                new Action(gossipUnitContext => ((WoWUnit)gossipUnitContext).Target()),
                                new Action(gossipUnitContext => QBCLog.Info("Interacting with \"{0}\" to start event.", ((WoWUnit)gossipUnitContext).SafeName)),
                                new Action(gossipUnitContext => ((WoWUnit)gossipUnitContext).Interact()),
                                new WaitContinue(_lagDuration, gossipUnitContext => GossipFrame.Instance.IsVisible, new ActionAlwaysSucceed()),
                                new WaitContinue(_delay_GossipDialogThrottle, gossipUnitContext => false, new ActionAlwaysSucceed()),
                                new Action(gossipUnitContext =>
                                {
                                    _gossipOptionIndex = 0;

                                    // If no dialog is expected, we're done...
                                    return StartEventGossipOptions[_gossipOptionIndex] < 0
                                        ? RunStatus.Failure
                                        : RunStatus.Success;
                                })
                            )),

                        // Choose appropriate gossip options...
                        // NB: If we get attacked while gossiping, and the dialog closes, then it will automatically be retried.
                        new Decorator(gossipUnitContext => (_gossipOptionIndex < StartEventGossipOptions.Length)
                                                            && (GossipFrame.Instance != null) && GossipFrame.Instance.IsVisible,
                                new Sequence(
                                    new Action(gossipUnitContext =>
                                    {
                                        GossipFrame.Instance.SelectGossipOption(StartEventGossipOptions[_gossipOptionIndex]);
                                        ++_gossipOptionIndex;

                                        // If Gossip completed, this behavior is done...
                                        // NB: This handles situations where the NPC closes the Gossip dialog instead of us.
                                        if (_gossipOptionIndex >= StartEventGossipOptions.Length)
                                        {
                                            _gossipBlacklist.Add((WoWUnit)gossipUnitContext, _duration_BlacklistGossip);
                                            return RunStatus.Failure;
                                        }

                                        return RunStatus.Success;
                                    }),
                                    new WaitContinue(_delay_GossipDialogThrottle, ret => false, new ActionAlwaysSucceed())
                                ))
                    )));
        }


        private void Utility_RotatePath(Queue<Vector3> path)
        {
            var frontPoint = path.Dequeue();
            path.Enqueue(frontPoint);
        }


        /// <summary>
        /// Unequivocally engages mob in combat.
        /// </summary>
        // 24Feb2013-08:11UTC chinajade
        private Composite UtilityBehavior_SpankMob(WoWUnitDelegate selectedTargetDelegate)
        {
            return new PrioritySelector(targetContext => selectedTargetDelegate(targetContext),
                new Decorator(targetContext => Query.IsViableForFighting((WoWUnit)targetContext),
                    new PrioritySelector(
                        new Decorator(targetContext => ((WoWUnit)targetContext).Distance > CharacterSettings.Instance.PullDistance,
                            new Action(targetContext => Navigator.MoveTo(((WoWUnit)targetContext).Location))),
                        new Decorator(targetContext => Me.CurrentTarget != (WoWUnit)targetContext,
                            new Sequence(
                                new Action(targetContext =>
                                {
                                    BotPoi.Current = new BotPoi((WoWUnit)targetContext, PoiType.Kill, QuestOrder.Instance.NavType);
                                    ((WoWUnit)targetContext).Target();
                                }),
                                 new Decorator(ctx => Me.Mounted, new ActionRunCoroutine(ctx => CommonCoroutines.LandAndDismount())))),
                        new Decorator(targetContext => !((WoWUnit)targetContext).IsTargetingMeOrPet,
                            new PrioritySelector(
                                // The NeedHeal and NeedCombatBuffs are part of legacy custom class support
                                // and pair with the Heal and CombatBuff virtual methods.  If a legacy custom class is loaded,
                                // HonorBuddy automatically wraps calls to Heal and CustomBuffs it in a Decorator checking those for you.
                                // So, no need to duplicate that work here.
                                new Decorator(ctx => RoutineManager.Current.HealBehavior != null,
                                    RoutineManager.Current.HealBehavior),
                                new Decorator(ctx => RoutineManager.Current.CombatBuffBehavior != null,
                                    RoutineManager.Current.CombatBuffBehavior),
                                RoutineManager.Current.CombatBehavior
                            ))
                    )));
        }

        private WoWUnit MobTargetingUs { get; set; }
        private Composite UtilityBehavior_SpankMobTargetingUs()
        {
            return new PrioritySelector(
                // If a mob is targeting us, deal with it immediately, so subsequent activities won't be interrupted...
                // NB: This can happen if we 'drag mobs' behind us on the way to our destination.
                new Decorator(context => !Query.IsViableForFighting(MobTargetingUs),
                    new Action(context =>
                    {
                        MobTargetingUs = FindNonFriendlyTargetingMeOrPet().OrderBy(u => u.DistanceSqr).FirstOrDefault();
                        return RunStatus.Failure;   // fall through
                    })),

                // Spank any mobs we find being naughty...
                new Decorator(context => MobTargetingUs != null,
                    UtilityBehavior_SpankMob(context => MobTargetingUs))
            );
        }
        #endregion // Behavior helpers


        #region Local Blacklist
        // The HBcore 'global' blacklist will also prevent looting.  We don't want that.
        // Since the HBcore blacklist is not built to instantiate, we have to roll our
        // own.  <sigh>
        private class LocalBlacklist
        {
            public LocalBlacklist(TimeSpan maxSweepTime)
            {
                _maxSweepTime = maxSweepTime;
                _stopWatchForSweeping.Start();
            }

            private readonly Dictionary<WoWGuid, DateTime> _blackList = new Dictionary<WoWGuid, DateTime>();
            private readonly TimeSpan _maxSweepTime;
            private readonly Stopwatch _stopWatchForSweeping = new Stopwatch();


            private void Add(WoWGuid guid, TimeSpan timeSpan)
            {
                if (_stopWatchForSweeping.Elapsed > _maxSweepTime)
                    RemoveExpired();

                _blackList[guid] = DateTime.Now.Add(timeSpan);
            }


            public void Add(WoWObject wowObject, TimeSpan timeSpan)
            {
                if (wowObject != null)
                    Add(wowObject.Guid, timeSpan);
            }


            private bool Contains(WoWGuid guid)
            {
                return (_blackList.ContainsKey(guid) && (_blackList[guid] > DateTime.Now));
            }


            public bool Contains(WoWObject wowObject)
            {
                return (wowObject != null) && Contains(wowObject.Guid);
            }


            private void RemoveExpired()
            {
                var now = DateTime.Now;

                List<WoWGuid> expiredEntries = (from key in _blackList.Keys
                                                where (_blackList[key] < now)
                                                select key).ToList();

                foreach (WoWGuid entry in expiredEntries)
                    _blackList.Remove(entry);

                _stopWatchForSweeping.Restart();
            }
        }
        #endregion


        #region TreeSharp Extensions
        private class CompositeThrottleContinue : DecoratorContinue
        {
            public CompositeThrottleContinue(TimeSpan throttleTime,
                                     Composite composite)
                : base(composite)
            {
                _throttle = new Stopwatch();
                _throttleTime = throttleTime;
            }


            protected override bool CanRun(object context)
            {
                if (_throttle.IsRunning && (_throttle.Elapsed < _throttleTime))
                { return false; }

                _throttle.Restart();
                return true;
            }

            private readonly Stopwatch _throttle;
            private readonly TimeSpan _throttleTime;
        }
        #endregion


        #region Path parsing
        // never returns null, but the returned Queue may be empty
        private Queue<Vector3> ParsePath(string pathElementName)
        {
            var descendants = Element.Descendants(pathElementName).Elements();
            var path = new Queue<Vector3>();

            foreach (var element in descendants.Where(elem => elem.Name == "Hotspot"))
            {
                var elementAsString = element.ToString();
                var isAttributeMissing = false;

                var xAttribute = element.Attribute("X");
                if (xAttribute == null)
                {
                    QBCLog.Error("Unable to locate X attribute for {0}", elementAsString);
                    isAttributeMissing = true;
                }

                var yAttribute = element.Attribute("Y");
                if (yAttribute == null)
                {
                    QBCLog.Error("Unable to locate Y attribute for {0}", elementAsString);
                    isAttributeMissing = true;
                }

                var zAttribute = element.Attribute("Z");
                if (zAttribute == null)
                {
                    QBCLog.Error("Unable to locate Z attribute for {0}", elementAsString);
                    isAttributeMissing = true;
                }

                if (isAttributeMissing)
                {
                    IsAttributeProblem = true;
                    continue;
                }

                var isParseProblem = false;

                float x;
                if (!float.TryParse(xAttribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                {
                    QBCLog.Error("Unable to parse X attribute for {0}", elementAsString);
                    isParseProblem = true;
                }

                float y;
                if (!float.TryParse(yAttribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                {
                    QBCLog.Error("Unable to parse Y attribute for {0}", elementAsString);
                    isParseProblem = true;
                }

                float z;
                if (!float.TryParse(zAttribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    QBCLog.Error("Unable to parse Z attribute for {0}", elementAsString);
                    isParseProblem = true;
                }

                if (isParseProblem)
                {
                    IsAttributeProblem = true;
                    continue;
                }

                path.Enqueue(new Vector3(x, y, z));
            }

            return path;
        }
        #endregion
    }
}

