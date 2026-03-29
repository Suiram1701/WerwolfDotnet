import { Fraction } from "../Api";

export const fractions: Record<Fraction, string> = {
    [Fraction.Village]: "Dorfbewohner",
    [Fraction.Werwolf]: "Werwölfe",
    [Fraction.WhiteWolf]: "Weißer Wolf",
    [Fraction.Lovers]: "Liebenden"
}

export const fractionWin: Record<Fraction, string> = {
    [Fraction.Village]: "Ihnen ist es gelungen alle Werwölfe zu töten.",
    [Fraction.Werwolf]: "Sie konnten alle Dorfbewohner auffressen.",
    [Fraction.WhiteWolf]: "Er konnte alle überlisten und blieb als einziger übrig.",
    [Fraction.Lovers]: "Sie blieben als einzige übrig."
}