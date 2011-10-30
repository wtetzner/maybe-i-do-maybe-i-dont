Maybe I Do, Maybe I Don't
---------

This allows you do chain methods in the Maybe monad.

Example:

    using org.bovinegenius.maybe;
    
    ...
    
    var x = Maybe.Do(() => Obj.Method().Property.Field.Method());

If anything in the chain returns null, then x will be null. Otherwise, x will contain the value of the expression.

You can also precompile an expression, and call it later:

    using org.bovinegenius.maybe;
    
    ...
    
    var someValue = Maybe.Compile(() => Obj.Method().Property.Field.Method());
    var x = someValue();
