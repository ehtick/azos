﻿using System;
using System.Net.Http;


namespace Azos.Client
{

  //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.0#consumption-patterns
  //Handle transient faults
  //Faults typically occur when external HTTP calls are transient.AddTransientHttpErrorPolicy allows a policy to be defined to handle transient errors. Policies configured with AddTransientHttpErrorPolicy handle the following responses:
  //    HttpRequestException
  //    HTTP 5xx
  //    HTTP 408
  //https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
  //https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
  //https://medium.com/@nuno.caneco/c-httpclient-should-not-be-disposed-or-should-it-45d2a8f568bc
  public class HttpTransport : DisposableObject, ITransportImplementation
  {
    protected internal HttpTransport(EndpointAssignment assignment)
    {
      m_Assignment = assignment;
    }

    protected readonly EndpointAssignment m_Assignment;
    protected readonly HttpClient m_Client;

    public EndpointAssignment Assignment => m_Assignment;
    public HttpEndpoint Endpoint => Assignment.Endpoint as HttpEndpoint;
    public HttpClient Client => Endpoint.Client;

  }
}
