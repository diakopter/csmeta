say("1..13");
my $foo = "ok 2 # accessing variables from outer scopes";
my $bar = "ok 4";
class Foo {
    my $bar = "ok 3";
    say("ok 1 # in class Foo");
    say($foo);
    say($bar);
    has str $attr;
    method foo(str $ok6 --> int) {
        say("ok 5");
        say($ok6);
        return 1;
    }
};
say($bar);
my $obj = Foo.new();
$obj.attr = "ok 7";
$obj.foo("ok 6");
say($obj.attr);

class Foor {
  has int $a;
  method new (--> Foor) {
    say("ok 8");
  }
};

class Bar is Foor {
  has int $b;
  method Baz (--> int) {
    say("ok 11");
	return 1;
  };

  method new (int $a, int $b --> Bar) {
    say("ok " ~ $a);
    say("ok " ~ $b);
    self.a = $a;
    self.b = $b;
  };
};

my $bark = Bar.new(9, 10);
$bark.Baz();
$bark.a += 3;
$bark.b += 3;
say("ok " ~ $bark.a);
say("ok " ~ $bark.b);

