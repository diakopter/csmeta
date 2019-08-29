say("1..10");
if (1>2) {say('not ok 1'); }
elsif (4==4)
{
say('ok 1');
};
 
 if (1>2) {say('not ok 2'); }
 elsif (4!=4)
 {
 say('not ok 2');
 }
 else
 {
 say('ok 2');
 };
 
 
 
 my $c=9;
 my $c2=0;
 while ($c>0)
 {
 $c2+=1;
 $c=($c-1);
 
 };
 if ($c2==9)
 {
 say('ok 3');
 }
 else
 {
 say('not ok 3');
 };
 
 $c=9;
 $c2=0;
 repeat while ($c>0)
 {
 $c2+=1;
 $c=($c-1);
 
 };
 if ($c2==9)
 {
 say('ok 4');
 }
 else
 {
 say('not ok 4');
 };
 
 $c=9;
 $c2=0;
 repeat until ($c==0)
 {
 $c2+=1;
 $c=($c-1);
 
 };
 if ($c2==9)
 {
 say('ok 5');
 }
 else
 {
 say('not ok 5');
 };
 
 $c=9;
 $c2=0;
 repeat 
 {
 $c2+=1;
 $c=($c-1);
 
 } while ($c>0);
 if ($c2==9)
 {
 say('ok 6');
 }
 else
 {
 say('not ok 6');
 };
 
  $c=9;
 $c2=0;
 repeat 
 {
 $c2+=1;
 $c=($c-1);
 
 } until ($c==0);
 if ($c2==9)
 {
 say('ok 7');
 }
 else
 {
 say('not ok 7');
 };

my $u=1;
 unless ($u==0) { $u+=1; };
 if ($u==2)
 {
 say('ok 8');
 }
 else
 {
 say('not ok 8');
 };
 my $g=0;
  loop
  {
  $g+=1;
  last;
  $g+=1;

  };
 
 if ($g==1)
 {
 say('ok 9');
 }
 else
 {
say('not ok 9'); 
 
 };
 my $c6=0;
 my $j=0;
 loop ($j=0;$j<9;$j+=1)
 {
 #$c6++;
 $c6+=1;
 };
 if ($c6==9)
 {
 say('ok 10');
 }
 else
 {
say('not ok 10'); 
 
 };
 
 


