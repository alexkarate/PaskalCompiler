Program Example;
var a,b:integer;
begin
  a:=1+2;
  b:=a div 3;
  a:=a + 0.5;
  b:= 'c';
  a:= True;
  b:= 'string';

  if b + 3 then
     b := 3
  else
  begin
     a := 5;
     b := 12 mod a;
  end;
  
  while a + 'str' do
     b:= (b + 1) * 2;
  a:=(a+b) div b + (a + b) * 3 - 2
end.