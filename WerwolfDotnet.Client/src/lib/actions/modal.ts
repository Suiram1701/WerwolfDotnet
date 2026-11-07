import { writable } from "svelte/store";
import type { Snippet } from "svelte";

export interface ModalOptions {
    title: string;
    content?: Snippet;
    contentText?: string;
    size?: "sm" | "lg" | "xl";
    confirmText?: string;
    allowHtmlText?: boolean;
    confirmColor?: string;
    canDismiss?: boolean;
    closeOnConfirm?: boolean;
    onConfirm?: () => void;
}
interface ModalState { options?: ModalOptions; visible: boolean }

export const modalState = writable<ModalState>({ visible: false });

export const modal = {
    show: (options: ModalOptions): void => modalState.set({ options, visible: true }),
    hide: (): void => modalState.update(s => ({ ...s, visible: false })),   // options bleiben, damit der Inhalt beim Ausblenden nicht verschwindet
};