say("1..2");
sub foo (int $a --> int) {
  say("ok " ~ $a);
  return 1;
};
foo(1); foo(2);
