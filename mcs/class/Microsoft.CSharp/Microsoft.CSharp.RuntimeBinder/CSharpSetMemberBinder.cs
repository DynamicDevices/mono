//
// CSharpSetMemberBinder.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using Compiler = Mono.CSharp;

namespace Microsoft.CSharp.RuntimeBinder
{
	class CSharpSetMemberBinder : SetMemberBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;
		
		public CSharpSetMemberBinder (string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (name, false)
		{
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
		
		public IList<CSharpArgumentInfo> ArgumentInfo {
			get {
				return argumentInfo;
			}
		}
		
		public override bool Equals (object obj)
		{
			var other = obj as CSharpSetMemberBinder;
			return other != null && other.Name == Name && other.callingContext == callingContext &&
				other.argumentInfo.SequenceEqual (argumentInfo);
		}

		public override int GetHashCode ()
		{
			return Extensions.HashCode (
				Name.GetHashCode (),
				callingContext.GetHashCode (),
				argumentInfo.GetHashCode ());
		}
		
		public override DynamicMetaObject FallbackSetMember (DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			var source = CSharpBinder.CreateCompilerExpression (argumentInfo[1], value);
			var expr = CSharpBinder.CreateCompilerExpression (argumentInfo [0], target);

			// Field assignment
			expr = new Compiler.MemberAccess (expr, Name);
			expr = new Compiler.SimpleAssign (expr, source);
			expr = new Compiler.Cast (new Compiler.TypeExpression (typeof (object), Compiler.Location.Null), expr); // TODO: ReturnType replace

			var restrictions = CSharpBinder.CreateRestrictionsOnTarget (target).Merge (CSharpBinder.CreateRestrictionsOnTarget (value));

			if (target.Value == null) {
				if (errorSuggestion != null)
					return errorSuggestion;

				var ex = CSharpBinder.CreateBinderException (target, "SetMember cannot perform binding on `null' value");
				return new DynamicMetaObject (ex, restrictions);
			}

			return CSharpBinder.Bind (target, expr, callingContext, restrictions, errorSuggestion);
		}
	}
}
