say('1..1');

sub n(string $name, Pattern $p --> Pattern) {
	return NamedGroup.new($name, $p);
};
sub empty(--> Pattern) {
	return Sprixel::Empty.new();
};
sub end(--> Pattern) {
	return End.new();
};
sub alt(Pattern $left, Pattern $right --> Pattern) {
	return Either.Discern($left, $right);
};
sub seq(Pattern $left, Pattern $right --> Pattern) {
	return Both.Discern($left, $right);
};
sub t(string $text --> Pattern) {
	return Literal.new($text);
};

my $g = Grammar.new("Foo");
{
$g.AddPattern("toplevel", seq(t('hi'),end()));

};

my Match $m = $g.Parse(UTF32String.new('hi'));

if $m.Success { say("ok 1") } else { say("nok 1") }
