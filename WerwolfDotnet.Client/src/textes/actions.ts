import {ActionType, Role} from "../Api";
import { roleNames } from "./roles";

export const actionNames: Readonly<Record<ActionType, string>> = {
    [ActionType.WerwolfVoting]: "Werwölfe erwachen",
    [ActionType.SeerSelection]: "Seher/-in erwacht",
}

export const actionDescriptions: Readonly<Record<ActionType, string>> = {
    [ActionType.WerwolfVoting]: "Wählt ein Opfer, was heute Nacht sterben soll.",
    [ActionType.SeerSelection]: "Wähle einen Spieler dessen Rolle du sehen möchtest.",
}

export const actionCompletions: Readonly<Record<ActionType, (args: string[]) => string>> = {
    [ActionType.WerwolfVoting]: args => `Ihr habt euch auf ${args[0]} geeinigt. Dieser Spieler wird am morgen tot sein (solange keine andere Rolle eingreift).`,
    [ActionType.SeerSelection]: args => `Du hast dich für ${args[0]} entschieden. Dieser Spieler hat die Rolle ${""}.`,
}