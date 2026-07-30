import { ActionType, Event, Fraction, type LogMessageDto, Role } from "../Api";
import { roleNames } from "./roles";
import { actionNames } from "./actions";
import { fractions } from "./fractions";

const eventMessages: Readonly<Record<Event, (args: any[]) => string>> = {
    [Event.Joined]: args => `${args[0]} ist dem Spiel beigetreten`,
    [Event.Left]: args => `${args[0]} hat das Spiel verlassen`,
    [Event.BecameGameMaster]: args => `${args[1]} ist der neue Game Master`,
    [Event.GameStarted]: args => "Spielt gestartet",
    [Event.GameStopped]: args => "Spiel gestoppt",
    [Event.GameWon]: args => `Spiel zu Ende, die ${fractions[args[0] as Fraction]}. Gewonnen haben ` + args.slice(1).join(', '),
    [Event.Voting]: args => `${actionNames[args[0] as ActionType]}(Auswahl): ${args.slice(1).join('; ')}`,
    [Event.Killed]: args => args[0] !== null ? `${args[0]} hat ${args[1]} getötet` : `${args[1]} ist gestorben`,
    [Event.Healed]: args => `${args[1]} wurde von ${args[0]} geheilt`,
    [Event.SawRole]: args => `${args[0]} hat die Rolle von ${args[1]} gesehen`,
    [Event.SeerApprenticeActive]: args => `${args[0]} ist nun der aktive Seher`,
    [Event.Protect]: args => `${args[0]} beschützt ${args[1]} diese Nacht`,
    [Event.SuccessfullyProtected]: args => `${args[0]} hat ${args[1]} erfolgreich beschützt`,
    [Event.FallInLove]: args => `${args[1]} und ${args[2]} sind nun ein Liebes paar`,
    [Event.SleepOver]: args => `${args[0]} hat bei ${args[1]} übernachtet`,
    [Event.VictimMissed]: args => `${args[0]} hat sein Ziel ${args[1]} verfehlt`,
    [Event.TurnedToWerwolf]: args => `${args[0]} hat ${args[1]} in einen Werwolf verwandelt`
}

export function renderMessage(message: LogMessageDto): string {
    const time = new Date(message.timeStamp!);
    const text = eventMessages[message.eventType!](message.args?.map(a => format(a)) ?? []);
    
    return `[${leadingZero(time.getHours())}:${leadingZero(time.getMinutes())}:${leadingZero(time.getSeconds())}]: ${text}`;
}

function leadingZero(value: number): string {
    return value.toString().padStart(2, '0');
}

function format(arg: any): any | null {
    if (typeof arg == 'number' || arg === null)
        return arg;
    else if ('name' in arg && 'role' in arg)
        return arg.name + (arg.role !== null ? ` (${roleNames[arg.role as Role]})` : "")
    else if ('key' in arg && 'value' in arg)
        return `${arg.key} -> ${arg.value.length > 0 ? arg.value.join(', ') : "(niemand)"}`;
    
    console.warn(`Couldn't find correct formatter for '${arg}'`);
    return null;
}