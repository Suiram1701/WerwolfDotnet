import { CauseOfDeath } from "../Api";

export const causeOfDeaths: Record<CauseOfDeath, (players: string[]) => string> = {
    [CauseOfDeath.None]: players => `D${players.length == 1 ? 'er' : 'ie'} Spieler ${players.join(', ')} wurde${players.length > 1 ? 'n' : ''}, jedoch tot aufgefunden.`,     // Only used when switching from night to day.
    [CauseOfDeath.WerwolfKilling]: players => `${players[0]} wurde nach der gemeinsamen Abstimmung hingerichtet.`,     // Only one possible
    [CauseOfDeath.WerwolfKill]: _ => "",        // Not meant to be public
    [CauseOfDeath.WitchPoisoning]: _ => "",     // --"--
    [CauseOfDeath.WitchExplosion]: players => `${players.join(', ')} ${players.length == 1 ? 'ist' : 'sind'} durch die Explosion des Hexenhauses gestorben.`
}