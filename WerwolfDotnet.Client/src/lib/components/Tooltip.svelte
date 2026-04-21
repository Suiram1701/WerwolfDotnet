<script lang="ts">
    import { tooltip } from "$lib/actions/tooltip";

    let { classNames = "", title, placement = "right" }: { classNames: string; title: string; placement: "top" | "bottom" | "left" | "right" } = $props();
</script>

<span class="material-symbols-outlined {classNames}" use:tooltip={{ title: title, placement: placement }}>info</span>