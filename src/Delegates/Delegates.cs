using System;
using System.Collections.Generic;

namespace dotNetExpress.Delegates;

public delegate void NextCallback(Exception err = null);

public delegate void ErrorCallback(Exception err, Request request, Response response, NextCallback next = null);

public delegate void MiddlewareCallback(Request request, Response response, NextCallback next = null);

public delegate void ListenCallback();

public delegate string RenderEngineCallback(string path, Dictionary<string, dynamic> locals);
