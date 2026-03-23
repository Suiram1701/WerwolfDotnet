import { CauseOfDeath } from "../Api";

export const causeOfDeaths: Record<CauseOfDeath, string> = {
    [CauseOfDeath.None]: "",     // Only internally used
    [CauseOfDeath.WerwolfKilling]: "Hinrichtung",
    [CauseOfDeath.WerwolfKill]: "Tot durch Werwölfe",
    [CauseOfDeath.WitchPoisoning]: "Von der Hexe vergiftet",
    [CauseOfDeath.WitchExplosion]: "Mit der Hexe explodiert",
    [CauseOfDeath.ShootByHunter]: "Vom Jäger erschossen",
    [CauseOfDeath.DeathByHearthBreak]: "Tot durch Liebeskummer"
}

// HTML allowed
export const causeOfDeathTemplates: Record<CauseOfDeath, (players: string[]) => string> = {
    [CauseOfDeath.None]: players => `<b>${players.join(', ')}</b> wurde${players.length > 1 ? 'n' : ''}, jedoch tot aufgefunden.`,
    [CauseOfDeath.WerwolfKilling]: players => `<b>${players[0]}</b> wurde nach der gemeinsamen Abstimmung hingerichtet.`,     // Only one possible
    [CauseOfDeath.WerwolfKill]: _ => "",        // Not meant to be public
    [CauseOfDeath.WitchPoisoning]: _ => "",     // --"--
    [CauseOfDeath.WitchExplosion]: players => `<b>${players.join(', ')}</b> ${players.length === 1 ? 'ist' : 'sind'} durch die Explosion des Hexenhauses gestorben.`,
    [CauseOfDeath.ShootByHunter]: players => `<b>${players.join(',')}</b> ${players.length === 1 ? 'hat der Jäger' : 'haben die Jäger'} mit in den Tod gerissen`,     // Only one player can die per hunter but just in case two hunters die.
    [CauseOfDeath.DeathByHearthBreak]: players => `<b>${players[0]}</b> hat sich aus Liebeskummer das Leben genommen.`     // Only one amor -> one amor death
}