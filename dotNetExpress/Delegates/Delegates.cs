using dotNetExpress.Exceptions;

namespace dotNetExpress.Delegates;

public delegate void NextCallback(ExpressException? err = null);

public delegate Task ErrorCallback(ExpressException err, Request req, Response res, NextCallback? next = null);

public delegate Task MiddlewareCallback(Request req, Response res, NextCallback? next = null);

public delegate void ListenCallback();

public delegate string RenderEngineCallback(string path, dynamic locals);
