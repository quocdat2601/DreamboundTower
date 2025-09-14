EXTERNAL Name(charName)
VAR heroName = ""          // khởi tạo rỗng
VAR earthExploded = false  // khởi tạo false

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
~ heroName = "Quang"
~ earthExploded = true
{ Name(heroName) }   // gọi external function
Tôi tên là {heroName}.
-> after_choice

== choose_dat ==
~ heroName = "Dat"
~ earthExploded = false
{ Name(heroName) }
Tôi tên là {heroName}.
-> after_choice


== after_choice ==
{Name("skibidi-chan")}
Okay, vay ra cau la {heroName} sao
{earthExploded:
    Trái đất đã nổ tung!
- else:
    cau cute vai
}
-> DONE

