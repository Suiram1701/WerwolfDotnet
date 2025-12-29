<script lang="ts">
    import { onMount, type Snippet } from "svelte";
    
    let { id, title, children, footer }: { id: string, title: string, children: Snippet, footer: Snippet } = $props();

    let instance: any;
    onMount(() => {
        // @ts-ignore (bootstrap imported from CDN)
        instance = new bootstrap.Modal("#" + id);
        return () => instance.dispose();
    });
    
    export function show(): void { instance.show(); }
    export function hide(): void { instance.hide(); }
</script>

<div {id} class="modal fade" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">{title}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">{@render children()}</div>
            <div class="modal-footer">{@render footer()}</div>
        </div>
    </div>
</div>