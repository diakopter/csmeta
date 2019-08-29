say("1..1");
class Foo {
    method bar(--> int) {
        say("ok 1 # calling methods on self works");
        return 1;
    }
    method foo(--> int) {
        self.bar();
        return 1;
    }
}
my Foo $foo = Foo.new();
$foo.foo();
