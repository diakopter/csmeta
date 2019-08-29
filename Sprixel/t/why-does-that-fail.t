# constructors/methods on generic types.

say("1..1");
########################
class P6object {
    method new() { # you need a default constructor
	};
    method missing(str $method --> P6object) {
        say("missing " ~ $method);
        return P6object.new();
    };
    method __new(P6object $capture --> P6object) {
        return self.missing("new");
    };
    method __FETCH(P6object $capture --> P6object) {
        return self.missing("FETCH");
    };
    method __postcircumfixColonParen_Thesis(P6object $capture --> P6object) {
        return self.missing("postcircumfix:( )");
    }
};
#########################
class X {
	method new() {}
};
class Y {
    has List[X] $positionals;
    method new() {
        my $list = List[X].new();
		my $x = X.new();
		$list.Add($x);
        #self.positionals = $list;
		say("ok " ~ $list.Count);
    };
};
##############################
my $y = Y.new();