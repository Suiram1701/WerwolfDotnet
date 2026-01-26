import {ActionType, Role} from "../Api";
import { roleNames } from "./roles";

export const actionNames: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Bürgermeisterwahl",
    [ActionType.WerwolfKilling]: "Vollstreckung",
    [ActionType.WerwolfSelection]: "Die Werwölfe erwachen",
    [ActionType.SeerSelection]: "Seher/-in erwacht",
    [ActionType.WitchHealSelection]: "Hexe erwacht (heilen)",
    [ActionType.WitchKillSelection]: "Hexe erwacht (töten)",
}

export const actionDescriptions: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Wähle einen Spieler aus dem Ihr besonders vertraut. Dessen Stimmte wird bei Abstimmungen mehr gewichtung haben.",
    [ActionType.WerwolfKilling]: "Der Tag ist vorbei. Einigt euch auf einen Spieler und richtet ihn gemeinsam hin. Danach bricht die Nacht an.",
    [ActionType.WerwolfSelection]: "Wählt ein Opfer, was heute Nacht sterben soll.",
    [ActionType.SeerSelection]: "Wähle einen Spieler dessen Rolle du sehen möchtest.",
    [ActionType.WitchHealSelection]: "Wähle eine Person, die du heilen möchtest (falls du möchtest).",
    [ActionType.WitchKillSelection]: "Wähle eine Person, die du mit deinem Todestrank töten möchtest (falls du möchtest).",
}

export const actionCompletions: Readonly<Record<ActionType, (args: string[]) => string>> = {
    [ActionType.MayorVoting]: args => args.length == 1
        ? `Ihr habt euch auf ${args[0]} geeinigt. Er wird einer neuer Bürgermeister sein und das Dorf anführen.`
        : "Ihr konntet euch auf keinen Bürgermeister einigen. Die Bürgermeisterwahl wird auf den nächsten Tag vertagt.",
    [ActionType.WerwolfKilling]: _ => "",
    [ActionType.WerwolfSelection]: args => args.length == 1
        ? `Ihr habt euch auf ${args[0]} geeinigt. Dieser Spieler wird am morgen tot sein (solange keine andere Rolle eingreift).`
        : "Ihr konntet euch auf keinen Spieler einigen, der Sterben soll.",
    [ActionType.SeerSelection]: args => `Du hast dich für ${args[0]} entschieden. Dieser Spieler hat die Rolle ${roleNames[Role[args[1] as keyof typeof Role]]}.`,
    [ActionType.WitchHealSelection]: _ => "",
    [ActionType.WitchKillSelection]: _ => "",
}