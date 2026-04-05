import {ActionType, Role} from "../Api";
import { roleNames } from "./roles";

export const actionNames: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Bürgermeisterwahl",
    [ActionType.NextMayorDecision]: "Entscheidung über nachfolgenden Bürgermeister",
    [ActionType.WerwolfAccuses]: "Anklage",
    [ActionType.WerwolfKilling]: "Vollstreckung",
    [ActionType.WerwolfSelection]: "Die Werwölfe erwachen",
    [ActionType.UrwolfSelection]: "Urwolf erwacht",
    [ActionType.WhiteWolfSelection]: "Weißer Werwolf erwacht",
    [ActionType.SeerSelection]: "Seher/-in erwacht",
    [ActionType.WitchHealSelection]: "Hexe erwacht (heilen)",
    [ActionType.WitchKillSelection]: "Hexe erwacht (töten)",
    [ActionType.HealerSelection]: "Heiler erwacht",
    [ActionType.HunterSelection]: "Du liegst im Sterben",
    [ActionType.AmorSelection]: "Amor schießt seinen Liebespfeil",
    [ActionType.VillageMattressSelection]: "Wahl des Übernachtungsortes"
}

export const actionDescriptions: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Wählt einen Spieler, dem ihr besonders vertraut, zum Bürgermeister. Dessen Stimme wird bei Abstimmungen die doppelte Gewichtung haben.",
    [ActionType.NextMayorDecision]: "Du liegst im Sterben. Wähle einen anderen Spieler, dem du besonders vertraust, welcher der nachfolgende Bürgermeister werden soll.",
    [ActionType.WerwolfAccuses]: "Der Abend bricht herein. Klagt die Personen an, die euch verdächtig vorkommen. Danach stehen alle Spieler, die zu den drei höchsten unterschiedlichen Stimmenzahlen gehören, in einer separaten Abstimmung. Dort kann 1 Spieler zur Hinrichtung ausgewählt werden.",
    [ActionType.WerwolfKilling]: "Folgende Spieler wurden angeklagt. Einigt euch auf einen Spieler und richtet ihn gemeinsam hin. Danach bricht die Nacht an.",
    [ActionType.WerwolfSelection]: "Wählt ein Opfer, was heute Nacht sterben soll.",
    [ActionType.UrwolfSelection]: "Entscheide, ob das Opfer der Werwölfe in einen Werwolf verwandelt werden soll, anstelle zu sterben. Pro Spiel kann nur 1 Opfer verwandelt werden.",
    [ActionType.WhiteWolfSelection]: "Entscheide dich für ein Opfer, welches du zusätzlich Töten möchtest. Dies ist nur jede zweite Runde möglich.",
    [ActionType.SeerSelection]: "Wähle einen Spieler, dessen Rolle du sehen möchtest.",
    [ActionType.WitchHealSelection]: "Wähle eine Person, die du heilen möchtest. Dies ist nur einmal pro Spiel möglich und du musst nicht zwingend heilen.",
    [ActionType.WitchKillSelection]: "Wähle eine Person, die du mit deinem Todestrank töten möchtest. Dies ist nur einmal pro Spiel möglich und du musst nicht zwingend töten.",
    [ActionType.HealerSelection]: "Wähle einen Spieler, den du diese Nacht vor den Werwölfen beschützen möchtest. Du kannst dieselbe Person nicht zweimal hintereinander beschützen.",
    [ActionType.HunterSelection]: "Wähle eine Person, die du mit in den Tod reißen möchtest.",
    [ActionType.AmorSelection]: "Wähle zwei Spieler, die sich unsterblich ineinander verlieben sollen. Stirbt ein Spieler des Liebespaares, dann stirbt der andere auch.",
    [ActionType.VillageMattressSelection]: "Wähle einen Spieler, bei dem du die Nacht verbringen möchtest. Du kannst nicht zweimal hintereinander bei derselben Person nächtigen."
}

// HTML allowed
export const actionCompletions: Partial<Record<ActionType, (args: string[]) => string>> = {
    [ActionType.MayorVoting]: args => args.length == 1
        ? `Ihr habt euch auf <b>${args[0]}</b> geeinigt. Er wird euer neuer Bürgermeister sein und das Dorf anführen. Bei folgenden Abstimmungen zählt seine Stimme doppelt.`
        : "Ihr konntet euch auf <b>keinen Bürgermeister</b> einigen. Die Bürgermeisterwahl wird auf den nächsten Tag verschoben.",
    [ActionType.WerwolfSelection]: args => args.length == 1
        ? `Ihr habt euch auf <b>${args[0]}</b> geeinigt. Dieser Spieler wird am Morgen tot sein, solange keine andere Rolle eingreift.`
        : "Ihr konntet euch auf <b>keinen Spieler</b> einigen, der sterben soll.",
    [ActionType.UrwolfSelection]: args => `Du hast dich für <b>${args[0]}</b> entschieden. Sofern er zuhause ist, wird er zu einem Werwolf.`,
    [ActionType.SeerSelection]: args => `Du hast dich für <b>${args[0]}</b> entschieden. Dieser Spieler hat die Rolle <b>${roleNames[Role[args[1] as keyof typeof Role]]}</b>.`,
}