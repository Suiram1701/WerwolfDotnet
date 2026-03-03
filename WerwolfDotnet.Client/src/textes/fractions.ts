import { Fraction } from "../Api";

export const fractions: Record<Fraction, string> = {
    [Fraction.Village]: "Dorfbewohner",
    [Fraction.Werwolf]: "Werwölfe",
    [Fraction.Lovers]: "Liebenden"
}

export const fractionWin: Record<Fraction, string> = {
    [Fraction.Village]: "Ihnen ist es gelungen alle Werwölfe zu töten.",
    [Fraction.Werwolf]: "Sie konnten alle Dorfbewohner auffressen.",
    [Fraction.Lovers]: "Sie konnten alle überlisten und sind als einzige übrig geblieben."
}