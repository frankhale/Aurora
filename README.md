##Aurora - An MVC web framework for .NET

NOTE: I've created a new branch where I'm rewriting Aurora. That is where
all new major work will happen for a while... =)

###Features

- Model View Controller based
- Apps can have a Front controller to intercept various events and perform 
	arbitrary logic before actions are invoked.
- Simple tag based view engine with master pages and partial views as well as 
	fragments.
- URL parameters bind to action method parameters automatically, no fiddling 
	with routes declarations.
- Posted forms binds to post models or action parameters automatically.
- Actions can have bound parameters that are bound to actions 
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
- OpenID authentication which is as easy as calling two methods. One to initiate 
	the login with the provider and then one to finalize authentication.
- Active Directory querying so you can authenticate your user against an Active 
	Directory user. Typically for use in client certificate authentication.

###Example Showing How To Use Aurora to Build an Application

For a comprehensive example of what Aurora can do and how to use it see the 
source code to Miranda my wiki application.

https://github.com/frankhale/Miranda

###Web.config configuration

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

###Note

Things are regressing a bit in relation to the Nuget packages and dependencies. 
I'm temporarily removing the Nuget package for Aurora and it's dependecies. 
There are some problems that I don't have time to focus on and would rather use
my time to work on the code base. What's more is that it's not that bad to just
include the few dependencies that Aurora has within it's code repository for 
now.

###Future

Here is a list of code considerations that I want to focus on:

- The framework initialization is a bit ridiculous and needs to be looked at.
- The middleware layer that was added hasn't been used and probably needs to be.
- The route handling code is still a bit brittle. I'd like to make this rock 
	solid and that is going to take some reengineering that will likely cause other
	code to have to be changed.
- The various framework assumptions and the way routes are processed I think 
	needs some work to make the code more sane and to eliminate huge code blocks
	in some of the functions.
- The model layer needs some love because it is not as robust as it needs to be.
- The view engine could stand a good looking over.

###License

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
Date: 21 November 2014
