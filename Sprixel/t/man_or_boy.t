
my $answers = List[int].new();
$answers.Add(1);
$answers.Add(0);
$answers.Add(-2);
$answers.Add(0);
$answers.Add(1);
$answers.Add(0);
$answers.Add(1);
$answers.Add(-1);
$answers.Add(-10);
$answers.Add(-30);
$answers.Add(-67);
$answers.Add(-138);
$answers.Add(-291);
$answers.Add(-642);
$answers.Add(-1446);
$answers.Add(-3250);
$answers.Add(-7244);
#$answers.Add(-16065);
#$answers.Add(-35601);
#$answers.Add(-78985);
#$answers.Add(-175416);
#$answers.Add(-389695);
#$answers.Add(-865609);
#$answers.Add(-1922362);
#$answers.Add(-4268854);
#$answers.Add(-9479595);
my $test_depth = $answers.Count - 1;

say("1.." ~ $test_depth);

sub A(int $k, Callable[:(--> int)] $x1, Callable[:(--> int)] $x2, Callable[:(--> int)] $x3, Callable[:(--> int)] $x4, Callable[:(--> int)] $x5 --> int) {
  my Callable[:(--> int)] $B;
  $B = sub (--> int) { $k-=1; return A($k, $B, $x1, $x2, $x3, $x4) };
  if $k <= 0 { return ($x4() + $x5()) };
  return ($B());
};

sub K(int $n --> Callable[:(--> int)]) { return sub (--> int) { return $n } };

my $i = 1;
loop (; $i <= $test_depth ; $i += 1 ) {
  my $result = A($i, K(1), K(-1), K(-1), K(1), K(0) );
  if $result != $answers[$i] { say("not ok # got " ~ ($result ~ (" but expected " ~ $answers[$i]))) }
  else { say("ok " ~ ($i ~ (" # Knuth's man_or_boy test at starting value " ~ ($i ~ (" got " ~ $result))))) }
};
