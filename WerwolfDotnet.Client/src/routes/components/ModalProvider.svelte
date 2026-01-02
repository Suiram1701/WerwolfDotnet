<script lang="ts">
    import { onMount, createRawSnippet, type Snippet } from "svelte";
    
    const modalId: string = "modalProvider-" + Math.floor(Math.random() * 100);
    let modalTitle: string | undefined = $state();
    let modalContent: Snippet | undefined = $state();
    let modalCanDismiss: boolean = $state(true);
    let modalConfirmText: string | undefined = $state();
    let modalConfirmColor: string = $state("primary");
    let modalOnConfirm: (() => void) | null;
    
    let instance: any;
    onMount(() => {
        // @ts-ignore (bootstrap imported from CDN)
        instance = new bootstrap.Modal("#" + modalId);
        return () => instance.dispose();
    });
    
    export function showSimple(title: string, content: string) {
        const snippet = createRawSnippet(() => {
            return { render: () => content };
        });
        show(title, snippet, false);
    }
    
    export function show(
        title: string,
        content: Snippet,
        canDismiss: boolean = true,
        confirmText: string = "Verstanden",
        confirmColor: string = "primary",
        onConfirm: (() => void) | null = null)
    {
        modalTitle = title;
        modalContent = content;
        modalCanDismiss = canDismiss;
        modalConfirmText = confirmText;
        modalConfirmColor = confirmColor;
        modalOnConfirm = onConfirm;
        instance.show();
    }
    
    export function hide() { instance.hide(); }
</script>

<div id={modalId} class="modal fade" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">{modalTitle}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">{@render modalContent?.()}</div>
            <div class="modal-footer">
                {#if modalCanDismiss}
                    <button class="btn btn-secondary" type="button" data-bs-dismiss="modal">Abrechen</button>
                {/if}
                <button class="btn btn-{modalConfirmColor}" type="button" onclick={() => {
                    modalOnConfirm?.();
                    hide();
                }}>{modalConfirmText}</button>
            </div>
        </div>
    </div>
</div>