Program Example;
var a,b:integer;
begin
  a:=2;
  b:=10;
  if b < a then
     writeln(b + ' is less than ' + a);
  else
     writeln(b + ' is greater or equals to ' + a);
  if b mod 3 = 0 then
  begin
    writeln(b);
    writeln('is divisible by 3');
  end;
  else
  begin
    writeln(b);
    writeln('is not divisible by 3');
  end;
end.