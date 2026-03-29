import { Role } from "../Api";

export const roleNames: Readonly<Record<Role, string>> = {
    [Role.WhiteWolf]: "Weißer Wolf",
    [Role.Urwolf]: "Urwolf",
    [Role.Werwolf]: "Werwolf",
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Dorfbewohner/-in",
    [Role.Seer]: "Seher/-in",
    [Role.SeerApprentice]: "Seher Lehrling",
    [Role.Witch]: "Hexe",
    [Role.Healer]: "Heiler",
    [Role.Hunter]: "Jäger",
    [Role.Amor]: "Amor",
    [Role.VillageMattress]: "Dorfmatratze",
    [Role.TwoSisters]: "zwei Schwestern",
    [Role.ThreeBrothers]: "drei Brüder",
    [Role.BearGuide]: "Bärenführer"
}

export const roleDescriptions: Readonly<Record<Role, string>> = {
    [Role.WhiteWolf]: "Du stimmst mit den anderen Werwölfen ab, jedoch spielst du insgeheim gegen. Dein Ziel ist es als einziger übrig zu bleiben. Du kannst jede zweite Nacht ein selbstgewähltes Opfer zusätzlich töten.",
    [Role.Urwolf]: "Du bist ein besonderer Werwolf, der sich mit den anderen Werwölfen jede Nacht für ein Opfer entscheidet und zusätzlich einen Dorfbewohner in einen Werwolf verwandeln kann.",
    [Role.Werwolf]: "Du bist ein Feind des Dorfes. Jede Nacht entscheidet ihr euch gemeinsam mit den anderen Werwölfen für ein Opfer.",
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Du bist ein ganz normaler Bewohner des Dorfes. Deine Aufgabe ist es, gemeinsam mit den anderen Dorfbewohnern die Werwölfe zu entlarven und zu hinzurichten.",
    [Role.Seer]: "Du besitzt eine besondere Gabe. Jede Nacht darfst du die Rolle eines Mitspielers erfahren und hilfst so dem Dorf. Aber Achtung. Es kann vorkommen, dass sie die Rolle eines Spielers im Verlauf eines Spiels ändert.",
    [Role.SeerApprentice]: "Du bist der Schüler des Sehers und erlernst dessen Fähigkeit, jede Nacht die Rolle eines Mitspielers zu erfahren. Sobald der Seher verstirbt bist du bereit dessen Position zu übernehmen.",
    [Role.Witch]: "Du kannst nachts einmal heilen und einmal töten. Nutze deine Tränke klug und bleibe unentdeckt.",
    [Role.Healer]: "Du hast die besondere Fähigkeit jede Nacht einen Spieler zu beschützen. Niemand wird in dieser Nacht in der Lage sein ihn zu töten. Wähle aber weise.",
    [Role.Hunter]: "Dorfbewohner mit einer Schusswaffe. Stirbt der Jäger, darf er einen Spieler seiner Wahl mit in den Tod nehmen.",
    [Role.Amor]: "In der ersten Nacht verbindest du zwei Spieler als Liebespaar. Stirbt einer, stirbt auch der andere.",
    [Role.VillageMattress]: "Du nächtigst jede Nacht bei einem anderen Spieler. Wird dieser von den Werwölfen angegriffen stirbst du auch, wird dein Haus angegriffen überlebst du.",
    [Role.TwoSisters]: "Du bist eine von zwei Schwestern. Ihr habt keine besondere Fähigkeit, allerdings eines wisst ihr: Ihr seid keine Werwölfe.",
    [Role.ThreeBrothers]: "Du bist einer von drei Brüdern. Ihr habt keine besondere Fähigkeit, jedoch eines wisst ihr sicher: Ihr seid keine Werwölfe.",
    [Role.BearGuide]: "Du bist passiv Teil des dorfes. Dein Bär brummt jeden Morgen oder nicht. Brummt er heißt das, dass direkt neben dir ein Werwolf sitzt. Stirbst du brummt er ein letztes mal."
}