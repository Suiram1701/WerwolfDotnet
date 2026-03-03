import { Role } from "../Api";

export const roleNames: Readonly<Record<Role, string>> = {
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Dorfbewohner/-in",
    [Role.Seer]: "Seher/-in",
    [Role.Werwolf]: "Werwolf",
    [Role.Witch]: "Hexe",
    [Role.Hunter]: "Jäger",
    [Role.Amor]: "Amor"
}

export const roleDescriptions: Readonly<Record<Role, string>> = {
    [Role.None]: "",     // Only used internally. Not a real role.
    [Role.Villager]: "Du bist ein ganz normaler Bewohner des Dorfes. Deine Aufgabe ist es, gemeinsam mit den anderen Dorfbewohnern die Werwölfe zu entlarven und zu hinzurichten.",
    [Role.Seer]: "Du besitzt eine besondere Gabe. Jede Nacht darfst du die Rolle eines Mitspielers erfahren und hilfst so dem Dorf. Aber Achtung. Es kann vorkommen, dass sie die Rolle eines Spielers im Verlauf eines Spiels ändert.",
    [Role.Werwolf]: "Du bist ein heimlicher Feind des Dorfes. Jede Nacht entscheidest du dich gemeinsam mit den anderen Werwölfen für ein Opfer – am Tag musst du unauffällig bleiben, um nicht enttarnt zu werden.",
    [Role.Witch]: "Du kannst nachts einmal heilen und einmal töten. Nutze deine Tränke klug und bleibe unentdeckt.",
    [Role.Hunter]: "Dorfbewohner mit einer Schusswaffe. Stirbt der Jäger, darf er einen Spieler seiner Wahl mit in den Tod nehmen.",
    [Role.Amor]: "In der ersten Nacht verbindest du zwei Spieler als Liebespaar. Stirbt einer, stirbt auch der andere."
}