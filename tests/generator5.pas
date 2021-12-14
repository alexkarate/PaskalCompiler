Program Example;
var a,b:integer;
begin
  a:=2;
  b:=100;
  while a < b do
  begin
    writeln(a);
    a:=a * a;
  end;
  writeln(a + ' is greater than ' + b);
end.