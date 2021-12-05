Program Example;
var a,b:integer;
begin
  a:= 1;
  b:= 2;
  if b = 1 
     b := 3
  else
  begin
     a := 5;
     b := 12 mod a;
  end;
  if b <> 1 then then
     b := 1;
  
  while b < a
     b:= (b + 1) * 2;
  while a > b do do
    a := b - 1;
end.