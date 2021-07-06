﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Net.Sockets;

using Azos.Apps;
using Azos.Collections;
using Azos.Conf;
using Azos.Instrumentation;
using Azos.IO.Sipc;
using Azos.Log;

namespace Azos.Apps.Hosting
{
  /// <summary>
  /// Provides services for managing subordinate processes
  /// </summary>
  public class GovernorDaemon : DaemonWithInstrumentation<IApplicationComponent>
  {
    public const string CONFIG_ACTIVATOR_SECTION = "activator";
    public const string CONFIG_APP_SECTION = "app";

    protected GovernorDaemon(IApplication app) : base(app)
    {
      m_Applications = new Registry<App>();
    }

    private Registry<App> m_Applications;
    private IAppActivator m_Activator;
    private GovernorSipcServer m_Server;
    private int m_ServerStartPort;
    private int m_ServerEndPort;

    public override bool InstrumentationEnabled { get; set; }
    public override string ComponentLogTopic => Sky.SysConsts.LOG_TOPIC_HOST_GOV;

    public IRegistry<App> Applications => m_Applications;

    [Config, ExternalParameter(CoreConsts.EXT_PARAM_GROUP_APP)]
    public int ServerStartPort
    {
      get => m_ServerStartPort;
      set => m_ServerStartPort = SetOnInactiveDaemon(value);
    }

    [Config, ExternalParameter(CoreConsts.EXT_PARAM_GROUP_APP)]
    public int ServerEndPort
    {
      get => m_ServerEndPort;
      set => m_ServerEndPort = SetOnInactiveDaemon(value);
    }

    /// <summary>
    /// Returns the assigned IPC port for active server or zero
    /// </summary>
    public int AssignedSipcServerPort
    {
      get
      {
        var srv = m_Server;
        if (Running && srv != null) return srv.AssignedPort;
        return 0;
      }
    }


    protected override void DoConfigure(IConfigSectionNode node)
    {
      base.DoConfigure(node);

      m_Applications.Clear();

      if (node == null) return;

      var nActivator = node[CONFIG_ACTIVATOR_SECTION];
      m_Activator = FactoryUtils.MakeDirectedComponent<IAppActivator>(this, nActivator, typeof(ProcessAppActivator), new []{ nActivator });

      foreach(var napp in node.ChildrenNamed(CONFIG_APP_SECTION))
      {
        var app = FactoryUtils.MakeDirectedComponent<App>(this, napp, typeof(App), new []{ napp });
        if (!m_Applications.Register(app))
        {
          throw new AppHostingException("Duplicate application id: `{0}`".Args(app.Name));
        }
      }
    }

    protected override void DoStart()
    {
      base.DoStart();
      m_Server = new GovernorSipcServer(this, m_ServerStartPort, m_ServerEndPort);
      m_Server.Start();

      if (m_Applications.Count == 0)
        WriteLogFromHere(MessageType.Warning, "No applications registered");

      m_Applications.ForEach(app => m_Activator.StartApplication(app));
    }
    protected override void DoSignalStop()
    {
      m_Applications.ForEach(app => m_Activator.StopApplication(app));
      base.DoSignalStop();
    }

    protected override void DoWaitForCompleteStop()
    {
      DisposeAndNull(ref m_Server);
      base.DoWaitForCompleteStop();
    }
  }
}
