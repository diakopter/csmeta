say("1..2");

# you can't augment a class that's already closed (P6object - since it's defined in C# already)
# I'll give it an error message once predeclarations are working, since it'll catch them.

#class P6object {
#    method missing(str $method --> P6object) {
#        return P6object.new()
#    }
#    method a(P6object $capture --> P6object) {
#        return self.missing("a")
#    }
#    method b(P6object $capture --> P6object) {
#        return self.missing("a")
#    }
#    method c(P6object $capture --> P6object) {
#        return self.missing("a")
#    }
#}

say("ok 1");

class P6object2 {
    method missing(str $method --> P6object2) {
        return P6object2.new()
    }
    method a(P6object2 $capture --> P6object2) {
        return self.missing("a")
    }
    method b(P6object2 $capture --> P6object2) {
        return self.missing("a")
    }
    method c(P6object2 $capture --> P6object2) {
        return self.missing("a")
    }
}

say("ok 2");