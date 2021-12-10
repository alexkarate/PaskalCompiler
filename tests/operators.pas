Program Example;
var a,b:integer;
begin
  a:=1+2;
  b:=a div 3;
  if b = 1 then
     b := 3
  else
  begin
     a := 5;
     b := 12 mod a;
  end;
  
  while b < a do
     b:= (b + 1) * 2;
  a:=(a+b) div b + (a + b) * 3 - 2;
end.