import type { Action } from "svelte/action";

export interface TooltipOptions {
    title: string;
    placement: "top" | "bottom" | "left" | "right";
}

export const tooltip: Action<HTMLElement, TooltipOptions> = (node: HTMLElement, options: TooltipOptions) => {
    // @ts-ignore
    let instance: any = new bootstrap.Tooltip(node, options);
    
    return {
        update(newOptions: TooltipOptions) {
            instance.dispose();
            // @ts-ignore
            instance = new bootstrap.Tooltip(node, newOptions);
        },
        destroy() {
            instance.dispose();
        }
    }
}