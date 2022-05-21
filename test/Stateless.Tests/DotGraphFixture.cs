// #define WRITE_DOTS_TO_FOLDER

using System;
using System.Collections.Generic;

using Stateless.Graph;
using Stateless.Reflection;

using Xunit;

namespace Stateless.Tests;

public class DotGraphFixture
{
#if WRITE_DOTS_TO_FOLDER
    static readonly string DestinationFolder = "c:\\temp\\";
#endif

    bool IsTrue() => true;

    void OnEntry()
    {

    }

    void OnExit()
    {

    }

    enum Style
    {
        UML
    }

    static readonly string suffix = Environment.NewLine
        + $" init [label=\"\", shape=point];" + Environment.NewLine
        + $" init -> \"A\"[style = \"solid\"]" + Environment.NewLine
        + "}";

    static string Prefix(Style style)
    {
        string s;

        s = "digraph {\n"
            + "compound=true;\n"
            + "node [shape=Mrecord]\n"
            + "rankdir=\"LR\"\n";

        return s.Replace("\n", Environment.NewLine);
    }

    static string Box(Style style, string label, List<string> entries = null, List<string> exits = null)
    {
        string b;

        List<string> es = new();
        if (entries != null)
        {
            foreach (string entry in entries)
            {
                es.Add("entry / " + entry);
            }
        }
        if (exits != null)
        {
            foreach (string exit in exits)
            {
                es.Add("exit / " + exit);
            }
        }

        if (es.Count == 0)
        {
            b = $"\"{label}\" [label=\"{label}\"];\n";
        }
        else
        {
            b = $"\"{label}\"" + " [label=\"" + label + "|" + string.Join("\\n", es) + "\"];\n";
        }

        return b.Replace("\n", Environment.NewLine);
    }

    static string Decision(Style style, string nodeName, string label)
    {
        string b;

        b = $"\"{nodeName}\"" + " [shape = \"diamond\", label = \"" + label + "\"];\n";

        return b.Replace("\n", Environment.NewLine);
    }

    static string Line(string from, string to, string label)
    {
        string s = "\n\"" + from + "\" -> \"" + to
            + "\" [style=\"solid\"";

        if (label != null)
        {
            s += ", label=\"" + label + "\"";
        }

        s += "];";

        return s.Replace("\n", Environment.NewLine);
    }

    static string Subgraph(Style style, string graphName, string label, string contents)
    {
        if (style != Style.UML)
        {
            throw new Exception("WRITE MORE CODE");
        }

        string s = "\n"
            + "subgraph \"cluster" + graphName + "\"\n"
            + "\t{\n"
            + "\tlabel = \"" + label + "\"\n";

        s = s.Replace("\n", Environment.NewLine)
            + contents          // \n already replaced with NewLine
            + "}" + Environment.NewLine;

        return s;
    }

    [Fact]
    public void SimpleTransition()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A") + Box(Style.UML, "B") + Line("A", "B", "X") + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .Permit(Trigger.X, State.B);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "SimpleTransition.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void SimpleTransitionUML()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A") + Box(Style.UML, "B") + Line("A", "B", "X") + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .Permit(Trigger.X, State.B);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "SimpleTransitionUML.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void TwoSimpleTransitions()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A") + Box(Style.UML, "B") + Box(Style.UML, "C")
            + Line("A", "B", "X")
            + Line("A", "C", "Y")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .Permit(Trigger.X, State.B)
            .Permit(Trigger.Y, State.C);

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void WhenDiscriminatedByAnonymousGuard()
    {
        static bool anonymousGuard()
        {
            return true;
        }

        string expected = Prefix(Style.UML) + Box(Style.UML, "A") + Box(Style.UML, "B")
            + Line("A", "B", "X [" + InvocationInfo.DefaultFunctionDescription + "]")
            + suffix;
        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .PermitIf(Trigger.X, State.B, anonymousGuard);
        sm.Configure(State.B);

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void WhenDiscriminatedByAnonymousGuardWithDescription()
    {
        static bool anonymousGuard()
        {
            return true;
        }

        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A") + Box(Style.UML, "B")
            + Line("A", "B", "X [description]")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .PermitIf(Trigger.X, State.B, anonymousGuard, "description");

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "WhenDiscriminatedByAnonymousGuardWithDescription.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void WhenDiscriminatedByNamedDelegate()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A") + Box(Style.UML, "B")
            + Line("A", "B", "X [IsTrue]")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .PermitIf(Trigger.X, State.B, IsTrue);

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void WhenDiscriminatedByNamedDelegateWithDescription()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A") + Box(Style.UML, "B")
            + Line("A", "B", "X [description]")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .PermitIf(Trigger.X, State.B, IsTrue, "description");
        sm.Configure(State.B);
        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void DestinationStateIsDynamic()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A")
            + Decision(Style.UML, "Decision1", "Function")
            + Line("A", "Decision1", "X") + suffix;

        StateMachine<State, Trigger> sm = new(State.A);
        sm.Configure(State.A)
            .PermitDynamic(Trigger.X, () => State.B);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "DestinationStateIsDynamic.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void DestinationStateIsCalculatedBasedOnTriggerParameters()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A")
            + Decision(Style.UML, "Decision1", "Function")
            + Line("A", "Decision1", "X") + suffix;

        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<int> trigger = sm.SetTriggerParameters<int>(Trigger.X);
        sm.Configure(State.A)
            .PermitDynamic(trigger, i => i == 1 ? State.B : State.C);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "DestinationStateIsCalculatedBasedOnTriggerParameters.dot", dotGraph);
#endif
        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void OnEntryWithAnonymousActionAndDescription()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A", new List<string> { "enteredA" }) + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnEntry(() => { }, "enteredA");

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "OnEntryWithAnonymousActionAndDescription.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void OnEntryWithNamedDelegateActionAndDescription()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A", new List<string> { "enteredA" }) + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnEntry(OnEntry, "enteredA");

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void OnExitWithAnonymousActionAndDescription()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A", null, new List<string> { "exitA" }) + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnExit(() => { }, "exitA");

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void OnExitWithNamedDelegateActionAndDescription()
    {

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnExit(OnExit, "exitA");

        string expected = Prefix(Style.UML) + Box(Style.UML, "A", null, new List<string> { "exitA" }) + suffix;
        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
        expected = Prefix(Style.UML) + Box(Style.UML, "A", null, new List<string> { "exitA" }) + suffix;
        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void TransitionWithIgnore()
    {
        // Ignored triggers do not appear in the graph
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A") + Box(Style.UML, "B")
            + Line("A", "B", "X")
            + Line("A", "A", "Y")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .Ignore(Trigger.Y)
            .Permit(Trigger.X, State.B);

        Assert.Equal(expected, UmlDotGraph.Format(sm.GetInfo()));
    }

    [Fact]
    public void OnEntryWithTriggerParameter()
    {
        string expected = Prefix(Style.UML) + Box(Style.UML, "A", new List<string> { "OnEntry" })
            + Box(Style.UML, "B") + Box(Style.UML, "C")
            + Line("A", "B", "X / BX")
            + Line("A", "C", "Y / TestEntryActionString [IsTriggerY]")
            + Line("A", "B", "Z [IsTriggerZ]")
            + suffix;

        static bool anonymousGuard()
        {
            return true;
        }

        StateMachine<State, Trigger> sm = new(State.A);
        StateMachine<State, Trigger>.TriggerWithParameters<string> parmTrig = sm.SetTriggerParameters<string>(Trigger.Y);

        sm.Configure(State.A)
            .OnEntry(() => { }, "OnEntry")
            .Permit(Trigger.X, State.B)
            .PermitIf(Trigger.Y, State.C, anonymousGuard, "IsTriggerY")
            .PermitIf(Trigger.Z, State.B, anonymousGuard, "IsTriggerZ");

        sm.Configure(State.B)
            .OnEntryFrom(Trigger.X, TestEntryAction, "BX");

        sm.Configure(State.C)
            .OnEntryFrom(parmTrig, TestEntryActionString);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());
#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "OnEntryWithTriggerParameter.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void SpacedUmlWithSubstate()
    {
        string StateA = "State A";
        string StateB = "State B";
        string StateC = "State C";
        string StateD = "State D";
        string TriggerX = "Trigger X";
        string TriggerY = "Trigger Y";

        string expected = Prefix(Style.UML)
                       + Subgraph(Style.UML, StateD, $"{StateD}\\n----------\\nentry / Enter D",
                           Box(Style.UML, StateB)
                           + Box(Style.UML, StateC))
                       + Box(Style.UML, StateA, new List<string> { "Enter A" }, new List<string> { "Exit A" })
                       + Line(StateA, StateB, TriggerX) + Line(StateA, StateC, TriggerY)
                       + Environment.NewLine
                       + $" init [label=\"\", shape=point];" + Environment.NewLine
                       + $" init -> \"{StateA}\"[style = \"solid\"]" + Environment.NewLine
                       + "}";

        StateMachine<string, string> sm = new("State A");

        sm.Configure(StateA)
            .Permit(TriggerX, StateB)
            .Permit(TriggerY, StateC)
            .OnEntry(TestEntryAction, "Enter A")
            .OnExit(TestEntryAction, "Exit A");

        sm.Configure(StateB)
            .SubstateOf(StateD);
        sm.Configure(StateC)
            .SubstateOf(StateD);
        sm.Configure(StateD)
            .OnEntry(TestEntryAction, "Enter D");

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());
#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "UmlWithSubstate.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void UmlWithSubstate()
    {
        string expected = Prefix(Style.UML)
            + Subgraph(Style.UML, "D", "D\\n----------\\nentry / EnterD",
                Box(Style.UML, "B")
                + Box(Style.UML, "C"))
            + Box(Style.UML, "A", new List<string> { "EnterA" }, new List<string> { "ExitA" })
            + Line("A", "B", "X") + Line("A", "C", "Y")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .Permit(Trigger.X, State.B)
            .Permit(Trigger.Y, State.C)
            .OnEntry(TestEntryAction, "EnterA")
            .OnExit(TestEntryAction, "ExitA");

        sm.Configure(State.B)
            .SubstateOf(State.D);
        sm.Configure(State.C)
            .SubstateOf(State.D);
        sm.Configure(State.D)
            .OnEntry(TestEntryAction, "EnterD");

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());
#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "UmlWithSubstate.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void UmlWithDynamic()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A")
            + Box(Style.UML, "B")
            + Box(Style.UML, "C")
            + Decision(Style.UML, "Decision1", "DestinationSelector")
            + Line("A", "Decision1", "X")
            + Line("Decision1", "B", "X [ChoseB]")
            + Line("Decision1", "C", "X [ChoseC]")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .PermitDynamic(Trigger.X, DestinationSelector, null, new DynamicStateInfos { { State.B, "ChoseB" }, { State.C, "ChoseC" } });

        sm.Configure(State.B);
        sm.Configure(State.C);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());
#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "UmlWithDynamic.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    [Fact]
    public void TransitionWithIgnoreAndEntry()
    {
        string expected = Prefix(Style.UML)
            + Box(Style.UML, "A", new List<string> { "DoEntry" })
            + Box(Style.UML, "B", new List<string> { "DoThisEntry" })
            + Line("A", "B", "X")
            + Line("A", "A", "Y")
            + Line("B", "B", "Z / DoThisEntry")
            + suffix;

        StateMachine<State, Trigger> sm = new(State.A);

        sm.Configure(State.A)
            .OnEntry(TestEntryAction, "DoEntry")
            .Ignore(Trigger.Y)
            .Permit(Trigger.X, State.B);

        sm.Configure(State.B)
            .OnEntry(TestEntryAction, "DoThisEntry")
            .PermitReentry(Trigger.Z);

        string dotGraph = UmlDotGraph.Format(sm.GetInfo());

#if WRITE_DOTS_TO_FOLDER
        System.IO.File.WriteAllText(DestinationFolder + "TransitionWithIgnoreAndEntry.dot", dotGraph);
#endif

        Assert.Equal(expected, dotGraph);
    }

    private void TestEntryAction() { }
    private void TestEntryActionString(string val) { }
    private State DestinationSelector() => State.A;
}
