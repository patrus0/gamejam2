@echo off
set p from_branch=Vvedite imya vetki, kotoruyu khotite vlit v tekushchuyu
git merge %from_branch%
echo Slianie vypolneno. Esli est konflikty – razreshite ikh vruchnuyu.
pause
