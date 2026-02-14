import {ActionType, Role} from "../Api";
import { roleNames } from "./roles";

export const actionNames: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Bürgermeisterwahl",
    [ActionType.NextMayorDecision]: "Nachfolgender Bürgermeister",
    [ActionType.WerwolfKilling]: "Vollstreckung",
    [ActionType.WerwolfSelection]: "Die Werwölfe erwachen",
    [ActionType.SeerSelection]: "Seher/-in erwacht",
    [ActionType.WitchHealSelection]: "Hexe erwacht (heilen)",
    [ActionType.WitchKillSelection]: "Hexe erwacht (töten)",
    [ActionType.HunterSelection]: "Du bist am Sterben",
    [ActionType.AmorSelection]: "Amor schießt"
}

export const actionDescriptions: Readonly<Record<ActionType, string>> = {
    [ActionType.MayorVoting]: "Wähle einen Spieler aus dem Ihr besonders vertraut. Dessen Stimmte wird bei Abstimmungen mehr gewichtung haben.",
    [ActionType.NextMayorDecision]: "Du bist am sterben. Wähle einen anderen Spieler aus dem Ihr besonders vertraut und das Amt fortführen soll.",
    [ActionType.WerwolfKilling]: "Der Tag ist vorbei. Einigt euch auf einen Spieler und richtet ihn gemeinsam hin. Danach bricht die Nacht an.",
    [ActionType.WerwolfSelection]: "Wählt ein Opfer, was heute Nacht sterben soll.",
    [ActionType.SeerSelection]: "Wähle einen Spieler dessen Rolle du sehen möchtest.",
    [ActionType.WitchHealSelection]: "Wähle eine Person, die du heilen möchtest (falls du möchtest).",
    [ActionType.WitchKillSelection]: "Wähle eine Person, die du mit deinem Todestrank töten möchtest (falls du möchtest).",
    [ActionType.HunterSelection]: "Wähle einer Person, die du mit in den Tot reißen möchtest mit deinem Gewehr.",
    [ActionType.AmorSelection]: "Wähle zwei Spieler, die du miteinander Verlieben möchtest."
}

// HTML allowed
export const actionCompletions: Partial<Record<ActionType, (args: string[]) => string>> = {
    [ActionType.MayorVoting]: args => args.length == 1
        ? `Ihr habt euch auf <b>${args[0]}</b> geeinigt. Er wird euer neuer Bürgermeister sein und das Dorf anführen.`
        : "Ihr konntet euch auf <b>keinen Bürgermeister</b> einigen. Die Bürgermeisterwahl wird auf den nächsten Tag vertagt.",
    [ActionType.WerwolfSelection]: args => args.length == 1
        ? `Ihr habt euch auf <b>${args[0]}</b> geeinigt. Dieser Spieler wird am morgen tot sein (solange keine andere Rolle eingreift).`
        : "Ihr konntet euch auf <b>keinen Spieler</b> einigen, der Sterben soll.",
    [ActionType.SeerSelection]: args => `Du hast dich für <b>${args[0]}</b> entschieden. Dieser Spieler hat die Rolle <b>${roleNames[Role[args[1] as keyof typeof Role]]}</b>.`,
}