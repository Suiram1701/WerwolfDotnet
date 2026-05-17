import { Role } from "../Api";

export const roleNames: Readonly<Record<Role, string>> = {
    [Role.WhiteWolf]: "Weißer Wolf",
    [Role.Urwolf]: "Urwolf",
    [Role.Werwolf]: "Werwolf",
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Dorfbewohner/-in",
    [Role.Seer]: "Seher/-in",
    [Role.SeerApprentice]: "Seherlehrling",
    [Role.Witch]: "Hexe",
    [Role.Healer]: "Heiler",
    [Role.Hunter]: "Jäger",
    [Role.Amor]: "Amor",
    [Role.VillageMattress]: "Dorfmatratze",
    [Role.TwoSisters]: "Die zwei Schwestern",
    [Role.ThreeBrothers]: "Die drei Brüder",
    [Role.BearGuide]: "Bärenführer"
}

export const roleDescriptions: Readonly<Record<Role, string>> = {
    [Role.WhiteWolf]: "Du stimmst mit den anderen Werwölfen ab, jedoch spielst du insgeheim gegen sie. Dein Ziel ist es, als einziger Spieler am Ende übrig zu bleiben. Du kannst jede zweite Nacht ein selbstgewähltes Opfer zusätzlich töten.",
    [Role.Urwolf]: "Du bist ein besonderer Werwolf, der sich mit den anderen Werwölfen jede Nacht für ein Opfer entscheidet und zusätzlich einmal pro Spiel ein gewähltes Opfer der Werwölfe in einen Werwolf verwandeln kann.",
    [Role.Werwolf]: "Du bist ein Feind des Dorfes. Jede Nacht entscheidest du dich gemeinsam mit den anderen Werwölfen für ein Opfer.",
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Du bist ein ganz normaler Bewohner des Dorfes. Deine Aufgabe ist es, gemeinsam mit den anderen Dorfbewohnern die Werwölfe zu entlarven und hinzurichten.",
    [Role.Seer]: "Du besitzt eine besondere Gabe. Jede Nacht darfst du die Rolle eines Mitspielers erfahren und hilfst so dem Dorf. Aber Achtung: Bei bestimmten Rollen kann es vorkommen, dass sich die Rolle des Spielers im Verlauf eines Spiels ändert.",
    [Role.SeerApprentice]: "Du bist der Schüler des Sehers und erlernst dessen Fähigkeit, jede Nacht die Rolle eines Mitspielers zu erfahren. Sobald der Seher stirbt, bist du bereit, seine Position zu übernehmen.",
    [Role.Witch]: "Du kannst nachts einmal pro Spiel einen Spieler heilen und einmal einen Spieler töten. Nutze deine Tränke klug und bleibe unentdeckt.",
    [Role.Healer]: "Du hast die besondere Fähigkeit, jede Nacht einen Spieler zu beschützen. Niemand wird in dieser Nacht in der Lage sein, diesen Spieler zu töten. Wähle aber weise.",
    [Role.Hunter]: "Du bist ein Dorfbewohner mit einer Schusswaffe. Stirbst du, kannst du einen Spieler deiner Wahl mit in den Tod reißen.",
    [Role.Amor]: "In der ersten Nacht wählst du zwei Spieler, die sich unsterblich ineinander verlieben. Stirbt einer der beiden, dann stirbt auch der andere.",
    [Role.VillageMattress]: "Du nächtigst jede Nacht bei einem anderen Spieler. Wird dieser getötet, stirbst du auch. Wird dein Haus angegriffen, während du woanders schläfst, überlebst du.",
    [Role.TwoSisters]: "Du bist eine von zwei Schwestern. Ihr habt keine besondere Fähigkeit. Nur eines wisst ihr sicher: Ihr seid beide keine Werwölfe.",
    [Role.ThreeBrothers]: "Du bist einer von drei Brüdern. Ihr habt keine besondere Fähigkeit. Nur eines wisst ihr sicher: Ihr seid alle keine Werwölfe.",
    [Role.BearGuide]: "Du bist Teil des Dorfes und besitzt einen Bär. Dein Bär kann am Morgen brummen. Wenn er brummt, heißt das, dass direkt neben dir (oberhalb oder unterhalb in der Spielerliste) ein Werwolf sitzt. Stirbst du, brummt der Bär ein letztes Mal."
}