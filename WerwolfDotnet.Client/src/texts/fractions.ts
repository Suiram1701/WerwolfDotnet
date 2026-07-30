import { Fraction } from "../Api";

export const fractions: Record<Fraction, string> = {
    [Fraction.Village]: "Dorfbewohner",
    [Fraction.Werwolf]: "Werwölfe",
    [Fraction.WhiteWolf]: "Weißer Wolf",
    [Fraction.Lovers]: "Liebespaar"
}

export const fractionWin: Record<Fraction, string> = {
    [Fraction.Village]: "Dem Dorf ist es gelungen alle Werwölfe zu töten.",
    [Fraction.Werwolf]: "Die Werwölfe konnten alle Dorfbewohner auffressen.",
    [Fraction.WhiteWolf]: "Der weiße Wolf konnte alle überlisten und blieb als einziger Spieler übrig.",
    [Fraction.Lovers]: "Die Verliebten blieben als einzige Spieler übrig."
}