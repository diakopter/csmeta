say("1..2");
class Foo {
    method foo(int $a --> int) {
        say("not ok " ~ $a);
        return 1;
    };
};

class Bar is Foo {
    method foo(int $a --> int) {
        say("ok " ~ $a);
        return 1;
    };
	method baz(int $a --> int) {
		say("ok " ~ $a);
		return 1;
	};
};

my $foo = Bar.new(); # $foo is definitely a Bar variable, and methods will be resolved at compile-time as such.
$foo.foo(1);

my Foo $bar = $foo; # $foo is casted to a Foo at runtime, and the result is stored in $bar, so the following virtual call calls Bar's foo method.
$bar.foo(2);

#$bar.baz(3); # would fail, since the $bar variable is a Foo (even though the object stored in it is a Bar)
