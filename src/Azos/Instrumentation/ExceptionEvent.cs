/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azos.Data;
using Azos.Serialization.Bix;

namespace Azos.Instrumentation
{
  /// <summary>
  /// Represents an exception event recorded by instrumentation
  /// </summary>
  [Serializable]
  [Bix("2A09545F-B237-4008-BEE8-26508724AB89")]
  public class ExceptionEvent : Event, IErrorInstrument
  {
    /// <summary>
    /// Create event from exception instance
    /// </summary>
    public static void Record(IInstrumentation inst, Exception error)
    {
      if (inst!=null && inst.Enabled) inst.Record(new ExceptionEvent(error));
    }

    /// <summary>
    /// Create event from exception instance and source
    /// </summary>
    public static void Record(IInstrumentation inst, string source, Exception error)
    {
      if (inst != null && inst.Enabled) inst.Record(new ExceptionEvent(source, error));
    }

    /// <summary>
    /// Create event from exception instance as of utcTime
    /// </summary>
    public static void Record(IInstrumentation inst, string source, Exception error, DateTime utcTime)
    {
      if (inst != null && inst.Enabled) inst.Record(new ExceptionEvent(source, error, utcTime));
    }

    private ExceptionEvent() {}

    protected ExceptionEvent(Exception error) : base() { ExceptionType = error.GetType().FullName; }

    protected ExceptionEvent(string source, Exception error) : base(source) { ExceptionType = error.GetType().FullName; }

    protected ExceptionEvent(string source, Exception error, DateTime utcTime) : base(source, utcTime) { ExceptionType = error.GetType().FullName; }


    [Field]
    public string ExceptionType { get; set;}

    [NonSerialized]
    private Dictionary<string, int> m_Errors;

    protected override Datum MakeAggregateInstance() { return new ExceptionEvent() { m_Errors = new Dictionary<string, int>() }; }

    protected override void AggregateOne(Datum evt)
    {
      var eevt = evt as ExceptionEvent;
      if (eevt == null) return;

      if (m_Errors.ContainsKey(eevt.ExceptionType))
        m_Errors[eevt.ExceptionType] += 1;
      else
        m_Errors.Add(eevt.ExceptionType, 1);
    }

    protected override void SummarizeAggregation()
    {
      var sb = new StringBuilder();

      foreach (var s in m_Errors.OrderBy(p => -p.Value).Take(10).Select(p => p.Key))
      {
        sb.Append(s);
        sb.Append(", ");
      }

      ExceptionType = sb.ToString();
    }

    public override string ToString() => base.ToString() + " " + (ExceptionType ?? string.Empty);

  }
}
