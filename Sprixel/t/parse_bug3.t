class P6object {
	method new() {}
}

say("1..1");
class Z {
    has List[P6object] $positionals;
    method add_positional(P6object $pos --> int) {
        my $a = self.positionals;
		$a.Add($pos);
        return 1;
    };
};
say("ok 1");