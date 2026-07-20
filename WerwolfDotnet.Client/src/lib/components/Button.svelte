<script lang="ts">
    import { type HTMLButtonAttributes } from "svelte/elements";
    
    let { variant = "primary", class: className = undefined, icon = undefined, children, onclick, ...rest }:
        { variant?: string, icon?: string | undefined } & HTMLButtonAttributes = $props();
</script>

<button class="btn btn-{variant}{icon !== undefined ? ' d-flex justify-content-center align-items-center' : ''}{className !== undefined ? ` ${className}` : ''}"
        type="button" onclick={onclick} {...rest}>
    {#if icon !== undefined}
        <span class="material-symbols-outlined me-2">{icon}</span>
    {/if}
    {@render children?.()}
</button>