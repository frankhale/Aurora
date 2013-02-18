Aurora - An MVC web framework for .NET 
======================================

A small MVC web framework built on top of ASP.NET

Frank Hale <frankhale@gmail.com>

Features
--------

// - Model View Controller based 
// - Apps can have a Front controller to intercept various events and perform
//   arbitrary logic before actions are invoked.
// - Simple tag based view engine with master pages and partial views as well as
//   fragments. 
// - URL parameters bind to action method parameters automatically, no fiddling with
//   routes declarations. 
// - Posted forms binds to post models or action parameters automatically. 
// - Actions can have bound parameters that are bound to actions (dependency injection)
// - Actions can be segregated based on Get, Post, GetOrPost, Put and Delete action 
//   type and you can secure them with the ActionSecurity named parameter.
// - Actions can have filters with optional filter results that bind to action
//   parameters.  
// - Actions can have aliases. Aliases can also be added dynamically at runtime
//   along with default parameters.
// - Bundling/Minifying of Javascript and CSS.
//
// Aurora.Extra 
//
// - My fork of Massive ORM
// - HTML helpers
// - Plugin support (can be used by apps but is not integrated at all into the
//   framework pipeline.)
// - OpenID authentication which is as easy as calling two methods. One 
//   to initiate the login with the provider and then one to finalize 
//   authentication.
// - Active Directory querying so you can authenticate your user against an 
//   Active Directory user. Typically for use in client certificate 
//   authentication.
//
// Aurora.Misc
//
// - My fork of the Gravatar URL generator
//
 
License 
-------

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