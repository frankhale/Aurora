## Aurora - An MVC web framework for .NET

A tiny MVC framework for .NET

From now on I'm shipping Miranda with Aurora for convenience. There are some 
things I need to make better and those will come in time. As far as the 'rewrite'
branch I'm just gonna start merging that stuff back in here and not try to do some
massive rewrite in it's own branch because there is not really time for it and
this is mostly a toy framework for fun. I'll just explore any ideas from now on
here in the master branch.

Just a FYI, today (17 September 2018) is the first time I've messed with this in
over 4 years. Hopefully now that I haven't had my eyes on this code I can come
back and find areas that need changing. 

Things I like about this framework is that there are a lot of cool features
and it's not too hard to add new ones because the framework is not that large.

### Features

- Model View Controller based
- Apps can have a Front controller to intercept various events and perform 
	arbitrary logic before actions are invoked.
- Simple tag based view engine with master pages and partial views as well as 
	fragments.
- URL parameters bind to action method parameters automatically, no fiddling 
	with routes declarations.
- Posted forms binds to post models or action parameters automatically.
- Actions can have bound parameters that bind when an action is called
	(dependency injection)
- Actions can be segregated based on Get, Post, GetOrPost, Put and Delete action 
	type and you can secure them with the ActionSecurity named parameter.
- Actions can have filters with optional filter results that bind to action 
	parameters.  
- Actions can have aliases. Aliases can also be added dynamically at runtime 
	along with default parameters.
- Bundling/Minifying of Javascript and CSS.
- Html Helpers
- Plugin support (can be used by apps but is not integrated at all into the 
	framework pipeline.)


## Features That Used To Work But Have Not Been Tested In More Than 4 Years

NOTE: The DotNetOpenAuth library has split into multiple sub modules and I have
not tested with the updated DLL's. I have no idea if the code still works but 
plan to test sometime soon.

- OpenID authentication which is as easy as calling two methods. One to initiate 
	the login with the provider and then one to finalize authentication.
- Active Directory querying so you can authenticate your user against an Active 
	Directory user. Typically for use in client certificate authentication.

### Example Showing How To Use Aurora to Build an Application

Miranda is a very simple, crude and old application which demonstrates a very 
rudimentary wiki. The main purpose is to exercise Aurora's routing, models,
login and view features. It's purpose is to test features of Aurora.

### Web.config configuration

Aurora uses a custom IHttpHandler and IHttpModule so your web.config will need 
to contain the following:

```xml
<system.webServer>
  <handlers>
    <add verb="*" path="*" name="AspNetAdapterHandler" type="AspNetAdapter.AspNetAdapterHandler" />
  </handlers>
  <modules>
    <add type="AspNetAdapter.AspNetAdapterModule" name="AspNetAdapterModule" />
  </modules>
</system.webServer>
```

### Note

Things are regressing a bit in relation to the Nuget packages and dependencies. 
I'm temporarily removing the Nuget package for Aurora and it's dependecies. 
There are some problems that I don't have time to focus on and would rather use
my time to work on the code base. What's more is that it's not that bad to just
include the few dependencies that Aurora has within it's code repository for 
now.

### Future

Here is a list of code considerations that I want to focus on:

- The framework initialization is a bit ridiculous and needs to be looked at.
- The middleware layer that was added hasn't been used and probably needs to be.
- The route handling code is still a bit brittle. I'd like to make this rock 
	solid and that is going to take some reengineering that will likely cause other
	code to have to be changed.
- The model layer needs some love because it is not as robust as it needs to be.

### License

GNU GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
```
// This program is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more
// details.
//
// You should have received a copy of the GNU General Public License along with
// this program.  If not, see <http://www.gnu.org/licenses/>.
```

Frank Hale &lt;frankhale@gmail.com&gt;  
Date: 17 September 2018
