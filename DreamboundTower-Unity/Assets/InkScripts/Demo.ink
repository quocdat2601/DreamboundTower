EXTERNAL Name(charName)
{Name("???")}
Hello cau
tui ten la skibidi-chan
{Name("skibidi-chan")}
sau lung toi la bong toi-chan

{Name("Bong toi-chan")}
ta la bong toi-chan
muhahahahaha
skibididopdopyetyet

{Name("skibidi-chan")}
Ban ten la gi?

* [Toi ten la Quang] -> choose_quang
* [Toi ten la Dat]   -> choose_dat

== choose_quang ==
VAR heroName = "Quang"
VAR earthExploded = true
{Name(heroName)}
Toi ten la {heroName}.
-> after_choice

== choose_dat ==
 heroName = "Dat"
 earthExploded = false
{Name(heroName)}
Toi ten la {heroName}.
-> after_choice

== after_choice ==
{Name("skibidi-chan")}
Okay, tiep tuc nao...
{earthExploded:
    Trái đất đã nổ tung!
- else:
Trái đất vẫn tồn tại. Câu chuyện tiếp tục...
}
-> DONE

