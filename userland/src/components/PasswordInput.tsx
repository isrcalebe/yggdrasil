import { Eye, EyeOff } from "lucide-react";
import { type ComponentProps, useState } from "react";

import { Button } from "@yggdrasil/components/ui/button";
import { Input } from "@yggdrasil/components/ui/input";
import { cn } from "@yggdrasil/lib/utils";

/** A password field with a button to reveal what was typed: typos are easy to miss when every character is a dot. */
export function PasswordInput({ className, ...props }: Omit<ComponentProps<"input">, "type">) {
  const [visible, setVisible] = useState(false);

  return (
    <div className="relative">
      <Input {...props} type={visible ? "text" : "password"} className={cn("pr-9", className)} />
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        className="absolute top-0.5 right-0.5"
        aria-label="Show password"
        aria-pressed={visible}
        aria-controls={props.id}
        onClick={() => {
          setVisible(current => !current);
        }}
      >
        {visible ? <EyeOff aria-hidden /> : <Eye aria-hidden />}
      </Button>
    </div>
  );
}
