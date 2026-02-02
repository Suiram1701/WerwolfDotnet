<script lang="ts">
    import { beforeNavigate } from "$app/navigation";
    import { onMount, type Snippet } from "svelte";
    
    interface Modal {
        title: string;
        content?: Snippet;
        contentText?: string;
        confirmText?: string;
        allowHtmlText?: boolean;
        confirmColor?: string;
        canDismiss?: boolean;
        closeOnConfirm?: boolean;
        onConfirm?: () => void;
    }
    
    const id: string = "modalProvider-" + Math.floor(Math.random() * 100);
    let modal: Modal | undefined = $state();
    
    let instance: any;
    onMount(() => {
        // @ts-ignore (bootstrap imported from CDN)
        instance = new bootstrap.Modal("#" + id);
        return () => instance.dispose();
    });
    
    beforeNavigate(() => hide());
    
    export function show(options: Modal)
    {
        modal = options;
        instance.show();
    }
    
    export function hide() { instance.hide(); }     // Don't set modal to undefined. The Modal content will disappear before the modal it's self finished the animation
</script>

<div {id} class="modal fade" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">
                    {#if modal?.allowHtmlText ?? false}
                        {@html modal?.title}
                    {:else}
                        {modal?.title}
                    {/if}
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                {#if modal?.content}
                    {@render modal.content?.()}
                {:else if modal?.allowHtmlText ?? false}
                    {@html modal?.contentText}
                {:else}
                    {modal?.contentText}
                {/if}
            </div>
            <div class="modal-footer">
                {#if modal?.canDismiss ?? true}
                    <button class="btn btn-secondary" type="button" data-bs-dismiss="modal">Abrechen</button>
                {/if}
                <button class="btn btn-{modal?.confirmColor ?? 'primary'}" type="button" onclick={() => {
                    modal?.onConfirm?.()
                    if (modal?.onConfirm === undefined || modal?.closeOnConfirm)
                        hide();
                }}>
                    {#if modal?.allowHtmlText ?? false}
                        {@html modal?.confirmText ?? "Verstanden"}
                    {:else}
                        {modal?.confirmText ?? "Verstanden"}
                    {/if}
                </button>
            </div>
        </div>
    </div>
</div>