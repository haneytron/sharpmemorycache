SharpMemoryCache 1.0.0
===========


Operates identically to a regular .NET MemoryCache, except that it properly initiates trims at the configured polling interval based on your memory settings.

A spinoff of the Dache project.

**WEB:**   http://www.getdache.net

**EMAIL:** info@getdache.net


VERSION HISTORY
============================================


1.0.0
------------------

- Initial release


INSTALLATION INSTRUCTIONS
============================================


Just include the DLL in your project ([NuGet](http://www.nuget.org/packages/SharpMemoryCache)) and create a `new TrimmingMemoryCache(name, config)` instead of a `new MemoryCache(name, config)`. It will do the rest automatically!
