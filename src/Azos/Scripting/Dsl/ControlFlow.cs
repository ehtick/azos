﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Threading.Tasks;
using Azos.Collections;

using Azos.Conf;
using Azos.Data;
using Azos.Serialization.JSON;
using Azos.Text;

namespace Azos.Scripting.Dsl
{
  /// <summary>
  /// Defines an entry point. Entry points need to be used as independently-addressable steps which execution can start from
  /// </summary>
  public class EntryPoint : Step
  {
    public EntryPoint(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }
    protected override Task<string> DoRunAsync(JsonDataMap state) => null;
  }

  /// <summary>
  /// Defines a subroutine which is a StepRunner tree
  /// </summary>
  public sealed class Sub : EntryPoint
  {
    public const string CONFIG_SOURCE_SECTION = "source";

    public Sub(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx)
    {
      var nSource = cfg[CONFIG_SOURCE_SECTION].NonEmpty($"child section `/{CONFIG_SOURCE_SECTION}`");
      m_Body = FactoryUtils.Make<StepRunner>(nSource, typeof(StepRunner), new object[]{ runner.App, nSource, runner.GlobalState });
    }

    private StepRunner m_Body;

    protected override async Task<string> DoRunAsync(JsonDataMap state)
    {
      m_Body.SetResult(Runner.Result);
      var local = await m_Body.RunAsync().ConfigureAwait(false);
      state.Append(local, deep: true);
      Runner.SetResult(m_Body.Result);
      return null;
    }
  }


  /// <summary>
  /// Signals that the underlying `StepRunner` should no longer contiue processing subsequent steps.
  /// Can be paired with the "If" step provide "Halt and catch fire" stop execution logic.
  /// </summary>
  public sealed class Halt : Step
  {
    public Halt(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }
    protected override Task<string> DoRunAsync(JsonDataMap state) => throw new StepRunner.HaltSignal();
  }

  /// <summary>
  /// Transfers control to another step
  /// </summary>
  public sealed class Goto : Step
  {
    public Goto(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }

    [Config] public string Step { get; set; }

    protected override Task<string> DoRunAsync(JsonDataMap state)
    {
      var result = Eval(Step, state);
      return Task.FromResult(result);
    }
  }

  /// <summary>
  /// Calls a named subroutine
  /// </summary>
  public sealed class Call : Step
  {
    public Call(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }

    [Config] public string Sub { get; set; }

    protected override async Task<string> DoRunAsync(JsonDataMap state)
    {
      Sub.NonBlank("call sub name");
      var inner = new StepRunner(App, Runner.RootSource, Runner.GlobalState);
      inner.SetResult(Runner.Result);
      await inner.RunAsync(Eval(Sub, state)).ConfigureAwait(false);
      Runner.SetResult(inner.Result);
      return null;
    }
  }


  /// <summary>
  /// If statement
  /// </summary>
  public sealed class If : Step
  {
    public If(StepRunner runner, IConfigSectionNode cfg, int idx) : base(runner, cfg, idx) { }

    [Config] public string Condition { get; set; }
    [Config("then")] public IConfigSectionNode Then { get; set; }
    [Config("else")] public IConfigSectionNode Else { get; set; }

    protected override async Task<string> DoRunAsync(JsonDataMap state)
    {
      Condition.NonBlank("condition");
      var eval = new Evaluator(Eval(Condition, state));
      var got = eval.Evaluate(id => StepRunnerVarResolver.GetResolver(Runner, id, state)).AsBool();

      if (got)
      {
        Then.NonEmpty(nameof(Then));
        var then = new StepRunner(App, Then, Runner.GlobalState);
        await then.RunAsync().ConfigureAwait(false);
      }
      else
      {
        Else.NonEmpty(nameof(Else));
        var @else = new StepRunner(App, Else, Runner.GlobalState);
        await @else.RunAsync().ConfigureAwait(false);
      }

      return null;
    }
  }

}
