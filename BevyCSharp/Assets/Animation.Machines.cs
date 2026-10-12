namespace Bevy;

public static partial class Animation
{
    /// <summary>The machines running, by the model they run on.</summary>
    private static readonly Dictionary<Entity, RunningMachine> Machines = [];

    /// <summary>Runs a state machine on a model, which enters its starting state once the model arrives.</summary>
    /// <param name="scene">The entity the model's scene was spawned under.</param>
    /// <param name="machine">The states and transitions, read from a file or made in code.</param>
    /// <remarks>
    /// <para>
    /// The machine plays its clips through <see cref="Play"/>, so what <see cref="StateOf"/> reads
    /// and <see cref="AnimationFinished"/> says go on as before, and a clip played by hand on the
    /// same model is played over until the machine next takes a transition. A machine run on a
    /// model running one already replaces it, with its parameters, and starts again.
    /// </para>
    /// <para>
    /// It moves once a frame, as the engine's messages are posted, so a parameter set during a
    /// frame is read at the start of the next. A model despawned takes its machine with it. A clip
    /// the model does not have is found once the model arrives, said as an error on the log, and
    /// the machine stopped, rather than ending the frame.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The machine has no states, two of one name, or a start or a transition naming a state it
    /// does not have, or a condition on a parameter with no name.
    /// </exception>
    public static void Run(Entity scene, AnimationStateMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (ProblemWith(machine) is { } problem) throw new ArgumentException(problem, nameof(machine));

        Machines[scene] = new RunningMachine(machine);
    }

    /// <summary>Stops the machine a model runs, leaving its clip playing as it was.</summary>
    /// <returns>Whether one was running.</returns>
    public static bool StopMachine(Entity scene) => Machines.Remove(scene);

    /// <summary>The state a model's machine is in, or nothing where none runs or the model has not arrived.</summary>
    public static string? MachineStateOf(Entity scene) =>
        Machines.TryGetValue(scene, out var running) ? running.State?.Name : null;

    /// <summary>Sets a number a model's machine compares, such as its speed.</summary>
    /// <exception cref="InvalidOperationException">No machine runs on the model.</exception>
    public static void SetParameter(Entity scene, string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);
        Running(scene).Values[name] = value;
    }

    /// <summary>Sets a flag a model's machine reads, such as whether it stands on the ground.</summary>
    /// <exception cref="InvalidOperationException">No machine runs on the model.</exception>
    public static void SetParameter(Entity scene, string name, bool value) => SetParameter(scene, name, value ? 1f : 0f);

    /// <summary>
    /// Fires a trigger a model's machine waits on, such as a jump, which stays until a transition
    /// takes it.
    /// </summary>
    /// <exception cref="InvalidOperationException">No machine runs on the model.</exception>
    public static void Trigger(Entity scene, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Running(scene).Triggers.Add(name);
    }

    /// <summary>Moves every machine one step. Called once a frame by the app.</summary>
    internal static void AdvanceMachines(EcsWorld world)
    {
        if (Machines.Count == 0) return;

        foreach (var (scene, running) in Machines.ToList())
        {
            if (!world.IsAlive(scene))
            {
                Machines.Remove(scene);
                continue;
            }

            try
            {
                running.Advance(scene);
            }
            catch (ArgumentException error)
            {
                // A clip the model lacks, which only the model can say and only once it arrives.
                Log.Error($"The animation state machine on {scene} stopped. {error.Message}");
                Machines.Remove(scene);
            }
        }
    }

    /// <summary>Forgets every machine, for an app starting, whose entities are its own.</summary>
    internal static void ForgetMachines() => Machines.Clear();

    private static RunningMachine Running(Entity scene) =>
        Machines.TryGetValue(scene, out var running)
            ? running
            : throw new InvalidOperationException($"No animation state machine runs on {scene}. Animation.Run starts one.");

    /// <summary>What is wrong with a machine's names, or nothing.</summary>
    private static string? ProblemWith(AnimationStateMachine machine)
    {
        if (machine.States.Count == 0) return "The machine has no states.";

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in machine.States)
        {
            if (string.IsNullOrEmpty(state.Name)) return "A state has no name.";
            if (!names.Add(state.Name)) return $"Two states are called {state.Name}.";
        }

        if (machine.Start.Length > 0 && !names.Contains(machine.Start))
            return $"The machine starts in {machine.Start}, which is not one of its states.";

        foreach (var transition in machine.Transitions)
        {
            var label = $"The transition from {(transition.From.Length > 0 ? transition.From : "any state")} to {transition.To}";
            if (transition.From.Length > 0 && !names.Contains(transition.From)) return $"{label} leaves a state the machine does not have.";
            if (!names.Contains(transition.To)) return $"{label} enters a state the machine does not have.";
            if (transition.When != AnimationCondition.Finished && transition.Parameter.Length == 0)
                return $"{label} is taken {transition.When} on a parameter with no name.";
        }

        return null;
    }

    /// <summary>A machine on one model, with the state it is in and the parameters the game set.</summary>
    private sealed class RunningMachine(AnimationStateMachine machine)
    {
        public readonly Dictionary<string, float> Values = new(StringComparer.Ordinal);
        public readonly HashSet<string> Triggers = new(StringComparer.Ordinal);

        public AnimationMachineState? State { get; private set; }

        public void Advance(Entity scene)
        {
            if (State is null)
            {
                var start = machine.Start.Length > 0 ? StateNamed(machine.Start) : machine.States[0];
                Enter(scene, start, 0f);
                return;
            }

            var now = StateOf(scene);

            foreach (var transition in machine.Transitions)
            {
                if (transition.From.Length > 0 ? transition.From != State.Name : transition.To == State.Name) continue;
                if (!Holds(transition, now)) continue;

                if (transition.When == AnimationCondition.Triggered) Triggers.Remove(transition.Parameter);
                Enter(scene, StateNamed(transition.To), transition.Blend);
                return;
            }
        }

        private bool Holds(AnimationMachineTransition transition, AnimationState? now)
        {
            var value = Values.GetValueOrDefault(transition.Parameter);

            return transition.When switch
            {
                // Read off the state's own clip, since the frame after a move can still report the
                // clip left until Bevy's player has taken the new one. Entering a state plays its
                // clip from the start, so a repeating one has come round once it counts one.
                AnimationCondition.Finished => now is { } state
                    && state.Clip == State!.Clip
                    && (state.Finished || state.Completions > 0),
                AnimationCondition.True => value != 0f,
                AnimationCondition.False => value == 0f,
                AnimationCondition.Above => value > transition.Value,
                AnimationCondition.Below => value < transition.Value,
                AnimationCondition.Triggered => Triggers.Contains(transition.Parameter),
                _ => false,
            };
        }

        /// <summary>Plays a state's clip, and moves into the state once it plays.</summary>
        /// <remarks>
        /// A model that has not arrived plays nothing, and the state is left as it was so the next
        /// frame asks again, which is how the first state waits for the model.
        /// </remarks>
        private void Enter(Entity scene, AnimationMachineState state, float blend)
        {
            var settings = new AnimationSettings { Repeat = state.Repeat, Speed = state.Speed, Blend = blend };
            if (!Play(scene, state.Clip, settings)) return;

            State = state;
        }

        private AnimationMachineState StateNamed(string name) =>
            machine.States.Find(state => state.Name == name)
            ?? throw new ArgumentException($"The machine has no state called {name}.");
    }
}
