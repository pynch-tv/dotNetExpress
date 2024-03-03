using dotNetExpress.Exceptions;
using System;

namespace dotNetExpress.Delegates;

public delegate void NextCallback(ExpressException err = null);

public delegate void ErrorCallback(ExpressException err, Request request, Response response, NextCallback next = null);

public delegate void MiddlewareCallback(Request request, Response response, NextCallback next = null);

public delegate void ListenCallback();

public delegate string RenderEngineCallback(string path, dynamic locals);
