Program Types;
var a:integer;
    c:char;
    cc: string;
    d:real;
    e:boolean;
begin
  a:= (-1 - 1) * (5 mod 3);
  writeln(a);
  c:= 'c' - 'a' + 'A';
  writeln(c);
  cc:= a + ' a ' + a;
  writeln(cc);
  d:= 13 / a;
  writeln(d);
  e:= d > a;
  writeln(e);
end.